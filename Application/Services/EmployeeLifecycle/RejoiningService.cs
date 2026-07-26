using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.EmployeeLifecycle
{
    // Rejoining - Phase 5 (final) of the "Probation & Confirmation"
    // (Employee Lifecycle) module. See Domain/Entities/ProbationConfirmation.cs
    // Phase 1, Domain/Entities/PipRecord.cs Phase 2,
    // Domain/Entities/EmployeeTransfer.cs Phase 3,
    // Domain/Entities/EmployeeFeedback.cs Phase 4.
    //
    // UNLIKE Phases 1-3, this feature has NO maker-checker workflow (same
    // as Phase 4) - it is a SIMPLE, single-step action, gated purely by
    // Create permission on AppFeatureConstants.REJOINING (HR-only, not
    // self-service).
    //
    // RejoinAsync rehires a FORMER employee (RelievingDate != null,
    // IsDeleted == false): it snapshots the Employee's CURRENT
    // RelievingDate/JoiningDate into a new RejoiningHistory row (the audit
    // trail - EmployeeService itself keeps no history for these fields),
    // then clears RelievingDate and sets JoiningDate to the new value.
    // Nothing else on the Employee record is touched - IsDeleted,
    // EmploymentType, ConfirmationDate, ProbationEndDate, and all
    // Department/Designation/Company/Branch/ReportingManager fields are
    // deliberately left as-is. If org placement also needs to change after
    // a rejoin, that is a SEPARATE, subsequent Employee Transfer action -
    // out of scope here. Purely additive/parallel; EmployeeService.cs is
    // never modified by this service.
    public class RejoiningService : IRejoiningService
    {
        private readonly ApplicationDbContext _context;

        public RejoiningService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Eligible For Rejoin

        // Lightweight "picker" list of former employees - tenant-scoped,
        // !IsDeleted && RelievingDate != null, optional name/code search.
        // No permission gate at this layer, mirroring
        // ProbationConfirmationService.GetDueForReviewAsync.
        public async Task<List<RejoiningEligibleEmployeeDto>> GetEligibleForRejoinAsync(string tenantId, string? search)
        {
            var query = _context.Employees
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Where(x =>
                    x.TenantId == tenantId &&
                    !x.IsDeleted &&
                    x.RelievingDate != null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.FirstName + " " + x.LastName).Contains(term) ||
                    x.EmployeeCode.Contains(term));
            }

            var employees = await query
                .OrderByDescending(x => x.RelievingDate)
                .ToListAsync();

            return employees
                .Select(x => new RejoiningEligibleEmployeeDto
                {
                    EmployeeId = x.Id,
                    EmployeeName = $"{x.FirstName} {x.LastName}".Trim(),
                    EmployeeCode = x.EmployeeCode,
                    JoiningDate = x.JoiningDate,
                    RelievingDate = x.RelievingDate!.Value,
                    DepartmentName = x.Department?.Name,
                    DesignationName = x.Designation?.Name
                })
                .ToList();
        }

        #endregion

        #region Rejoin

        public async Task<RejoiningHistoryDto> RejoinAsync(RejoinEmployeeDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // HR-only authorization - anyone holding Create permission on
            // this feature; not a self-service action.
            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to rejoin an employee.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            if (employee.RelievingDate == null)
                throw new Exception("This employee has not exited and therefore cannot be rejoined.");

            if (dto.NewJoiningDate < employee.RelievingDate.Value)
                throw new Exception("New Joining Date cannot be before the employee's Relieving Date.");

            // Snapshot the Employee's CURRENT RelievingDate/JoiningDate
            // BEFORE mutating them - never caller-supplied - defense in
            // depth against stale/manipulated "previous" data, same
            // reasoning as EmployeeTransfer's From* snapshot fields.
            var previousRelievingDate = employee.RelievingDate.Value;
            var previousJoiningDate = employee.JoiningDate;

            var entity = new RejoiningHistory
            {
                Id = IDManager.GetNewId(new RejoiningHistory()),

                EmployeeId = employee.Id,

                PreviousRelievingDate = previousRelievingDate,
                PreviousJoiningDate = previousJoiningDate,
                NewJoiningDate = dto.NewJoiningDate,

                Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),

                ProcessedByUserId = actingUserId,
                ProcessedOn = DateTime.UtcNow,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.RejoiningHistories.Add(entity);

            // Mutate ONLY the exit marker and joining date on the live
            // Employee record - purely about clearing the exit + setting a
            // new join date. Deliberately NOT touched: IsDeleted,
            // EmploymentType, ConfirmationDate, ProbationEndDate, or any
            // Department/Designation/Company/Branch/ReportingManager
            // fields - if org placement also needs to change, that's a
            // SEPARATE subsequent Employee Transfer action, and a fresh
            // Probation Confirmation cycle can be triggered later
            // separately if warranted.
            employee.RelievingDate = null;
            employee.JoiningDate = dto.NewJoiningDate;

            employee.ModifiedOn = DateTime.UtcNow;
            employee.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Read

        private async Task<RejoiningHistory> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.RejoiningHistories
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Rejoining History record not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (RejoinAsync)
        // - no additional authorization check here.
        private async Task<RejoiningHistoryDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return (await AssembleDtoListAsync(new List<RejoiningHistory> { entity })).Single();
        }

        // HR-internal view: anyone holding View permission on REJOINING may
        // view any record tenant-wide - same permissive model as
        // EmployeeTransferService.GetByIdAsync/ProbationConfirmationService.GetByIdAsync.
        public async Task<RejoiningHistoryDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Rejoining History records.");

            var entity = await GetEntityByIdAsync(id, tenantId);
            return (await AssembleDtoListAsync(new List<RejoiningHistory> { entity })).Single();
        }

        // All RejoiningHistory rows for one employee - an employee could
        // rejoin more than once over their lifetime - tenant-scoped, newest
        // first. No permission gate at this layer, mirroring
        // EmployeeTransferService.GetTransferHistoryForEmployeeAsync.
        public async Task<List<RejoiningHistoryDto>> GetHistoryForEmployeeAsync(string employeeId, string tenantId)
        {
            var entities = await _context.RejoiningHistories
                .Include(x => x.Employee)
                .Where(x => x.TenantId == tenantId && x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        // HR audit list - all RejoiningHistory records tenant-wide, with
        // optional name/code search.
        public async Task<List<RejoiningHistoryDto>> GetAllAsync(string tenantId, string? search)
        {
            var query = _context.RejoiningHistories
                .Include(x => x.Employee)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Authorization Helpers

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the REJOINING
        // feature - same join shape as
        // EmployeeTransferService.HasPermissionAsync/
        // ProbationConfirmationService.HasPermissionAsync, scoped to this
        // feature's own FeatureId.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.REJOINING && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        // ProcessedByUserId stores the acting User.Id - resolve a display
        // name via the linked Employee if one exists, else fall back to the
        // account's Username. Mirrors
        // EmployeeTransferService.BuildUserNameMapAsync.
        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<List<RejoiningHistoryDto>> AssembleDtoListAsync(List<RejoiningHistory> entities)
        {
            var userNameMap = await BuildUserNameMapAsync(entities.Select(x => x.ProcessedByUserId));

            var list = new List<RejoiningHistoryDto>();

            foreach (var x in entities)
            {
                var dto = new RejoiningHistoryDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                    EmployeeCode = x.Employee?.EmployeeCode,

                    PreviousRelievingDate = x.PreviousRelievingDate,
                    PreviousJoiningDate = x.PreviousJoiningDate,
                    NewJoiningDate = x.NewJoiningDate,

                    Reason = x.Reason,

                    ProcessedByUserId = x.ProcessedByUserId,
                    ProcessedByName = userNameMap.TryGetValue(x.ProcessedByUserId, out var name) ? name : null,
                    ProcessedOn = x.ProcessedOn,

                    TenantId = x.TenantId,
                    CreatedOn = x.CreatedOn,
                    CreatedBy = x.CreatedBy
                };

                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
