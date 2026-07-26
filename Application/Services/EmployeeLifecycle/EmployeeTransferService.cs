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
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeLifecycle
{
    // Phase 3 of the "Probation & Confirmation" (Employee Lifecycle)
    // module - Employee Transfer. See Domain/Entities/EmployeeTransfer.cs.
    //
    // MAKER: anyone holding Create permission on
    // AppFeatureConstants.EMPLOYEE_TRANSFER (typically HR Manager) proposes
    // new Company/Branch/Department/Designation/ReportingManager values for
    // an Employee - not a self-service action. The FROM* snapshot is always
    // captured automatically from the Employee's CURRENT values at
    // proposal time (never caller-supplied) - defense in depth against
    // stale/manipulated "from" data.
    //
    // CHECKER: a DIFFERENT acting user (actingUserId != record.MakerId)
    // holding Approve permission on the same feature must Approve or
    // Reject the proposal - identical invariant to
    // ProbationConfirmationService/PipService (checked BEFORE any
    // permission check, no override).
    //
    // This EmployeeTransfer row IS the audit trail for organizational
    // changes made via this feature - EmployeeService.UpdateAsync itself
    // keeps no history for these fields. Purely additive/parallel;
    // EmployeeService.cs is never modified by this service.
    public class EmployeeTransferService : IEmployeeTransferService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeTransferService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Create

        public async Task<EmployeeTransferDto> CreateAsync(CreateEmployeeTransferDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Maker authorization - anyone holding Create permission on
            // this feature (typically HR Executive/HR Manager); not a
            // self-service action.
            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to propose an Employee Transfer.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive);

            if (employee == null)
                throw new Exception("Employee not found.");

            if (employee.RelievingDate != null)
                throw new Exception("This employee has already exited and cannot be proposed for a transfer.");

            // Snapshot FROM* automatically from the Employee's CURRENT
            // values - never caller-supplied. Defense in depth against
            // stale/manipulated "from" data.
            var fromCompanyId = employee.CompanyId;
            var fromBranchId = employee.BranchId;
            var fromDepartmentId = employee.DepartmentId;
            var fromDesignationId = employee.DesignationId;
            var fromReportingManagerId = employee.ReportingManagerId;

            var toCompanyId = string.IsNullOrWhiteSpace(dto.ToCompanyId) ? null : dto.ToCompanyId.Trim();
            var toBranchId = string.IsNullOrWhiteSpace(dto.ToBranchId) ? null : dto.ToBranchId.Trim();
            var toDepartmentId = string.IsNullOrWhiteSpace(dto.ToDepartmentId) ? null : dto.ToDepartmentId.Trim();
            var toDesignationId = string.IsNullOrWhiteSpace(dto.ToDesignationId) ? null : dto.ToDesignationId.Trim();
            var toReportingManagerId = string.IsNullOrWhiteSpace(dto.ToReportingManagerId) ? null : dto.ToReportingManagerId.Trim();

            // At least one "To" field must be non-null AND differ from its
            // "From" counterpart - reject a no-op transfer proposal.
            var hasChange =
                (toCompanyId != null && toCompanyId != fromCompanyId) ||
                (toBranchId != null && toBranchId != fromBranchId) ||
                (toDepartmentId != null && toDepartmentId != fromDepartmentId) ||
                (toDesignationId != null && toDesignationId != fromDesignationId) ||
                (toReportingManagerId != null && toReportingManagerId != fromReportingManagerId);

            if (!hasChange)
                throw new Exception("At least one proposed value must differ from the employee's current value - a no-op transfer cannot be proposed.");

            // Basic FK sanity for any provided "To" ids, mirroring how
            // other Create flows in this codebase validate FK existence.
            if (toCompanyId != null &&
                !await _context.Companies.AnyAsync(x => x.Id == toCompanyId && x.TenantId == tenantId && !x.IsDeleted))
                throw new Exception("The selected new Company does not exist.");

            if (toBranchId != null &&
                !await _context.Branches.AnyAsync(x => x.Id == toBranchId && x.TenantId == tenantId && !x.IsDeleted))
                throw new Exception("The selected new Branch does not exist.");

            if (toDepartmentId != null &&
                !await _context.Departments.AnyAsync(x => x.Id == toDepartmentId && x.TenantId == tenantId && !x.IsDeleted))
                throw new Exception("The selected new Department does not exist.");

            if (toDesignationId != null &&
                !await _context.Designations.AnyAsync(x => x.Id == toDesignationId && x.TenantId == tenantId && !x.IsDeleted))
                throw new Exception("The selected new Designation does not exist.");

            if (toReportingManagerId != null &&
                !await _context.Employees.AnyAsync(x => x.Id == toReportingManagerId && x.TenantId == tenantId && !x.IsDeleted))
                throw new Exception("The selected new Reporting Manager does not exist.");

            var entity = new EmployeeTransfer
            {
                Id = IDManager.GetNewId(new EmployeeTransfer()),

                EmployeeId = dto.EmployeeId,
                EffectiveDate = dto.EffectiveDate,
                Reason = dto.Reason,

                FromCompanyId = fromCompanyId,
                FromBranchId = fromBranchId,
                FromDepartmentId = fromDepartmentId,
                FromDesignationId = fromDesignationId,
                FromReportingManagerId = fromReportingManagerId,

                ToCompanyId = toCompanyId,
                ToBranchId = toBranchId,
                ToDepartmentId = toDepartmentId,
                ToDesignationId = toDesignationId,
                ToReportingManagerId = toReportingManagerId,

                MakerId = actingUserId,
                MakerActionOn = DateTime.UtcNow,

                Status = TransferStatus.PendingChecker,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.EmployeeTransfers.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Read

        private async Task<EmployeeTransfer> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.EmployeeTransfers
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Employee Transfer record not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (Create/
        // Approve/Reject) - no additional authorization check here.
        private async Task<EmployeeTransferDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return (await AssembleDtoListAsync(new List<EmployeeTransfer> { entity })).Single();
        }

        // HR-internal view: anyone holding View permission on
        // EMPLOYEE_TRANSFER may view any record tenant-wide - same
        // permissive model as ProbationConfirmationService.GetByIdAsync /
        // PipService.GetByIdAsync.
        public async Task<EmployeeTransferDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Employee Transfer records.");

            var entity = await GetEntityByIdAsync(id, tenantId);
            return (await AssembleDtoListAsync(new List<EmployeeTransfer> { entity })).Single();
        }

        public async Task<List<EmployeeTransferDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search)
        {
            var query = _context.EmployeeTransfers
                .Include(x => x.Employee)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransferStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

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

        // Convenience method for an employee-detail "transfer history" tab -
        // all EmployeeTransfer rows (any status) for one employee, newest
        // first. No permission gate at this layer, mirroring
        // GetDueForReviewAsync/GetActivePipsAsync's equivalent listing
        // methods.
        public async Task<List<EmployeeTransferDto>> GetTransferHistoryForEmployeeAsync(string employeeId, string tenantId)
        {
            var entities = await _context.EmployeeTransfers
                .Include(x => x.Employee)
                .Where(x => x.TenantId == tenantId && x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Maker-Checker Workflow

        public async Task<EmployeeTransferDto> ApproveAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.EmployeeTransfers
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (record == null)
                throw new Exception("Employee Transfer record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Checked BEFORE any permission check, with NO override (not
            // even for HR/Admin) - segregation of duties.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to approve Employee Transfer records.");

            if (record.Status != TransferStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be approved.");

            record.Status = TransferStatus.Approved;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = string.IsNullOrWhiteSpace(dto?.CheckerRemarks) ? null : dto.CheckerRemarks.Trim();

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            var employee = record.Employee
                ?? await _context.Employees.FirstOrDefaultAsync(x => x.Id == record.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found.");

            // Apply ONLY the non-null "To" fields onto the live Employee
            // record - null means "no change requested for this
            // dimension, keep the existing value". This EmployeeTransfer
            // row is itself the audit trail; EmployeeService is never
            // touched.
            if (!string.IsNullOrEmpty(record.ToCompanyId))
                employee.CompanyId = record.ToCompanyId;

            if (!string.IsNullOrEmpty(record.ToBranchId))
                employee.BranchId = record.ToBranchId;

            if (!string.IsNullOrEmpty(record.ToDepartmentId))
                employee.DepartmentId = record.ToDepartmentId;

            if (!string.IsNullOrEmpty(record.ToDesignationId))
                employee.DesignationId = record.ToDesignationId;

            if (!string.IsNullOrEmpty(record.ToReportingManagerId))
                employee.ReportingManagerId = record.ToReportingManagerId;

            employee.ModifiedOn = DateTime.UtcNow;
            employee.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        public async Task<EmployeeTransferDto> RejectAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.EmployeeTransfers
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (record == null)
                throw new Exception("Employee Transfer record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Same ordering as ApproveAsync: identity check first, no
            // override.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to act on Employee Transfer records.");

            if (record.Status != TransferStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be rejected.");

            if (string.IsNullOrWhiteSpace(dto?.CheckerRemarks))
                throw new Exception("Checker remarks are required to reject an Employee Transfer.");

            record.Status = TransferStatus.Rejected;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = dto.CheckerRemarks.Trim();

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            // No changes applied to the Employee record - the maker may
            // submit a new proposal afterward by calling CreateAsync
            // again, no explicit "resubmit" action needed.
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        // THE CORE MAKER-CHECKER INVARIANT: the acting user's own resolved
        // identity (their User.Id, i.e. actingUserId - NOT their linked
        // EmployeeId) must differ from record.MakerId. There is no
        // override for this - not even HR/Admin can check their own
        // submission.
        private static void EnsureCheckerIsNotMaker(EmployeeTransfer record, string actingUserId)
        {
            if (!string.IsNullOrEmpty(actingUserId) && actingUserId == record.MakerId)
                throw new UnauthorizedAccessException(
                    "The checker must be a different person than the maker - you cannot approve your own submission.");
        }

        #endregion

        #region Authorization Helpers

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the
        // EMPLOYEE_TRANSFER feature - same join shape as
        // ProbationConfirmationService.HasPermissionAsync /
        // PipService.HasPermissionAsync, scoped to this feature's own
        // FeatureId.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_TRANSFER && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        // MakerId/CheckerId store the acting User.Id - resolve a display
        // name via the linked Employee if one exists, else fall back to
        // the account's Username. Mirrors
        // ProbationConfirmationService.BuildUserNameMapAsync /
        // PipService.BuildUserNameMapAsync.
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

        // From*/To* Company/Branch/Department/Designation/ReportingManager
        // ids have no EF navigation property on EmployeeTransfer (see the
        // entity's remarks) - display names are resolved here via batch
        // ID->name lookups instead, one query per org-entity type across
        // ALL entities being assembled (avoids N+1).
        private async Task<Dictionary<string, string>> BuildCompanyNameMapAsync(IEnumerable<string?> ids)
        {
            var idList = ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<string, string>();

            var rows = await _context.Companies.Where(x => idList.Contains(x.Id)).ToListAsync();
            return rows.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task<Dictionary<string, string>> BuildBranchNameMapAsync(IEnumerable<string?> ids)
        {
            var idList = ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<string, string>();

            var rows = await _context.Branches.Where(x => idList.Contains(x.Id)).ToListAsync();
            return rows.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task<Dictionary<string, string>> BuildDepartmentNameMapAsync(IEnumerable<string?> ids)
        {
            var idList = ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<string, string>();

            var rows = await _context.Departments.Where(x => idList.Contains(x.Id)).ToListAsync();
            return rows.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task<Dictionary<string, string>> BuildDesignationNameMapAsync(IEnumerable<string?> ids)
        {
            var idList = ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<string, string>();

            var rows = await _context.Designations.Where(x => idList.Contains(x.Id)).ToListAsync();
            return rows.ToDictionary(x => x.Id, x => x.Name);
        }

        // For FromReportingManagerId/ToReportingManagerId - these are
        // Employee.Id values (not User.Id), unlike Maker/CheckerId, so this
        // is deliberately separate from BuildUserNameMapAsync.
        private async Task<Dictionary<string, string>> BuildEmployeeNameMapAsync(IEnumerable<string?> ids)
        {
            var idList = ids.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();
            if (idList.Count == 0)
                return new Dictionary<string, string>();

            var rows = await _context.Employees.Where(x => idList.Contains(x.Id)).ToListAsync();
            return rows.ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}".Trim());
        }

        private async Task<List<EmployeeTransferDto>> AssembleDtoListAsync(List<EmployeeTransfer> entities)
        {
            var userNameMap = await BuildUserNameMapAsync(entities.SelectMany(x => new[] { x.MakerId, x.CheckerId }));

            var companyNameMap = await BuildCompanyNameMapAsync(entities.SelectMany(x => new[] { x.FromCompanyId, x.ToCompanyId }));
            var branchNameMap = await BuildBranchNameMapAsync(entities.SelectMany(x => new[] { x.FromBranchId, x.ToBranchId }));
            var departmentNameMap = await BuildDepartmentNameMapAsync(entities.SelectMany(x => new[] { x.FromDepartmentId, x.ToDepartmentId }));
            var designationNameMap = await BuildDesignationNameMapAsync(entities.SelectMany(x => new[] { x.FromDesignationId, x.ToDesignationId }));
            var reportingManagerNameMap = await BuildEmployeeNameMapAsync(entities.SelectMany(x => new[] { x.FromReportingManagerId, x.ToReportingManagerId }));

            var list = new List<EmployeeTransferDto>();

            foreach (var x in entities)
            {
                var dto = new EmployeeTransferDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                    EmployeeCode = x.Employee?.EmployeeCode,

                    EffectiveDate = x.EffectiveDate,
                    Reason = x.Reason,

                    FromCompanyId = x.FromCompanyId,
                    FromCompanyName = companyNameMap.TryGetValue(x.FromCompanyId, out var fromCompanyName) ? fromCompanyName : null,

                    FromBranchId = x.FromBranchId,
                    FromBranchName = !string.IsNullOrEmpty(x.FromBranchId) && branchNameMap.TryGetValue(x.FromBranchId, out var fromBranchName) ? fromBranchName : null,

                    FromDepartmentId = x.FromDepartmentId,
                    FromDepartmentName = departmentNameMap.TryGetValue(x.FromDepartmentId, out var fromDepartmentName) ? fromDepartmentName : null,

                    FromDesignationId = x.FromDesignationId,
                    FromDesignationName = designationNameMap.TryGetValue(x.FromDesignationId, out var fromDesignationName) ? fromDesignationName : null,

                    FromReportingManagerId = x.FromReportingManagerId,
                    FromReportingManagerName = !string.IsNullOrEmpty(x.FromReportingManagerId) && reportingManagerNameMap.TryGetValue(x.FromReportingManagerId, out var fromRmName) ? fromRmName : null,

                    ToCompanyId = x.ToCompanyId,
                    ToCompanyName = !string.IsNullOrEmpty(x.ToCompanyId) && companyNameMap.TryGetValue(x.ToCompanyId, out var toCompanyName) ? toCompanyName : null,

                    ToBranchId = x.ToBranchId,
                    ToBranchName = !string.IsNullOrEmpty(x.ToBranchId) && branchNameMap.TryGetValue(x.ToBranchId, out var toBranchName) ? toBranchName : null,

                    ToDepartmentId = x.ToDepartmentId,
                    ToDepartmentName = !string.IsNullOrEmpty(x.ToDepartmentId) && departmentNameMap.TryGetValue(x.ToDepartmentId, out var toDepartmentName) ? toDepartmentName : null,

                    ToDesignationId = x.ToDesignationId,
                    ToDesignationName = !string.IsNullOrEmpty(x.ToDesignationId) && designationNameMap.TryGetValue(x.ToDesignationId, out var toDesignationName) ? toDesignationName : null,

                    ToReportingManagerId = x.ToReportingManagerId,
                    ToReportingManagerName = !string.IsNullOrEmpty(x.ToReportingManagerId) && reportingManagerNameMap.TryGetValue(x.ToReportingManagerId, out var toRmName) ? toRmName : null,

                    MakerId = x.MakerId,
                    MakerName = userNameMap.TryGetValue(x.MakerId, out var makerName) ? makerName : null,
                    MakerActionOn = x.MakerActionOn,
                    MakerRemarks = x.MakerRemarks,

                    Status = (int)x.Status,
                    StatusName = x.Status.ToString(),

                    CheckerId = x.CheckerId,
                    CheckerName = !string.IsNullOrEmpty(x.CheckerId) && userNameMap.TryGetValue(x.CheckerId, out var checkerName) ? checkerName : null,
                    CheckerActionOn = x.CheckerActionOn,
                    CheckerRemarks = x.CheckerRemarks,

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
