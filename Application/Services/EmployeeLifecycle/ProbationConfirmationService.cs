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
    // Foundational Maker-Checker (segregation-of-duties) service for the
    // Probation & Confirmation module - the FIRST feature in this codebase
    // to enforce "the approver must be a different person than whoever
    // proposed the action". Unlike the existing Reporting-Manager-or-HR/
    // Admin-override approval pattern used by WfhRequestService/
    // OnDutyRequestService/ShortLeaveRequestService, there is NO override
    // for the maker != checker identity check - not even HR/Admin can
    // check their own submission.
    //
    // MAKER: anyone holding Create permission on
    // AppFeatureConstants.PROBATION_CONFIRMATION (typically HR Manager) may
    // propose an outcome for any Employee's probation - not a self-service
    // action, Employees never propose their own confirmation.
    //
    // CHECKER: a DIFFERENT acting user (actingUserId != record.MakerId)
    // holding Approve permission on the same feature must Approve or
    // Reject the proposal - see ApproveAsync/RejectAsync for the exact
    // ordering (identity check BEFORE permission check).
    //
    // Later PIP Outcome / Employee Transfer agents replicating this
    // pattern: copy the field shape (MakerId/MakerActionOn/MakerRemarks/
    // Status/CheckerId/CheckerActionOn/CheckerRemarks), the
    // actingUserId != MakerId check ordering, and the HasPermissionAsync
    // join shape below (scoped to your own FeatureId constant) - do not
    // share this class or its enum.
    public class ProbationConfirmationService : IProbationConfirmationService
    {
        private readonly ApplicationDbContext _context;

        public ProbationConfirmationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // A 2-week lookahead window so HR can act on-or-before the actual
        // probation end date, not only after it has already passed.
        private const int DueForReviewLookaheadDays = 14;

        #region Due For Review

        public async Task<List<ProbationDueForReviewDto>> GetDueForReviewAsync(string tenantId, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Probation Confirmation records.");

            var today = DateTime.UtcNow.Date;
            var lookaheadCutoff = today.AddDays(DueForReviewLookaheadDays);

            var query = _context.Employees
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Where(x =>
                    x.TenantId == tenantId &&
                    !x.IsDeleted &&
                    x.RelievingDate == null &&
                    x.EmploymentType == EmploymentType.Probation &&
                    (x.ProbationEndDate == null || x.ProbationEndDate <= lookaheadCutoff))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.FirstName + " " + x.LastName).Contains(term) ||
                    x.EmployeeCode.Contains(term));
            }

            var employees = await query.ToListAsync();

            // Exclude employees who already have an open (PendingChecker)
            // proposal, so HR doesn't see - and potentially double-submit
            // for - the same employee while a decision is already in
            // flight.
            var employeeIds = employees.Select(x => x.Id).ToList();

            var pendingEmployeeIds = (await _context.ProbationConfirmations
                    .Where(x =>
                        x.TenantId == tenantId &&
                        !x.IsDeleted &&
                        employeeIds.Contains(x.EmployeeId) &&
                        x.Status == ProbationConfirmationStatus.PendingChecker)
                    .Select(x => x.EmployeeId)
                    .ToListAsync())
                .ToHashSet();

            var result = employees
                .Where(x => !pendingEmployeeIds.Contains(x.Id))
                .Select(x =>
                {
                    int? daysRemaining = x.ProbationEndDate.HasValue
                        ? (int?)(x.ProbationEndDate.Value.Date - today).Days
                        : null;

                    return new ProbationDueForReviewDto
                    {
                        EmployeeId = x.Id,
                        EmployeeName = $"{x.FirstName} {x.LastName}".Trim(),
                        EmployeeCode = x.EmployeeCode,
                        DepartmentName = x.Department?.Name,
                        DesignationName = x.Designation?.Name,
                        JoiningDate = (DateTime)x.JoiningDate,
                        ProbationEndDate = x.ProbationEndDate,
                        DaysRemaining = daysRemaining,
                        IsOverdue = daysRemaining.HasValue && daysRemaining.Value < 0
                    };
                })
                .OrderBy(x => x.ProbationEndDate ?? DateTime.MaxValue)
                .ToList();

            return result;
        }

        #endregion

        #region CRUD

        public async Task<ProbationConfirmationDto> CreateAsync(CreateProbationConfirmationDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Maker authorization - anyone holding Create permission on
            // this feature (typically HR Executive/HR Manager); not a
            // self-service action.
            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to propose a Probation Confirmation.");

            var employee = await _context.Employees
                .Include(x => x.Designation)
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            if (employee.RelievingDate != null)
                throw new Exception("This employee has already exited and cannot be proposed for a probation decision.");

            if (!Enum.IsDefined(typeof(ProbationRecommendation), dto.Recommendation))
                throw new Exception("Invalid recommendation.");

            var recommendation = (ProbationRecommendation)dto.Recommendation;

            if (recommendation == ProbationRecommendation.Extend && !dto.ExtendedProbationEndDate.HasValue)
                throw new Exception("Extended Probation End Date is required when the recommendation is Extend.");

            // Avoid a second open proposal for the same employee while one
            // is already awaiting checker action.
            var hasPending = await _context.ProbationConfirmations
                .AnyAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted &&
                    x.Status == ProbationConfirmationStatus.PendingChecker);

            if (hasPending)
                throw new Exception("This employee already has a Probation Confirmation proposal pending checker action.");

            // Snapshot the probation window as of now - prefer the
            // Employee's own ProbationEndDate if already set, else fall
            // back to JoiningDate + Designation.ProbationPeriodMonths.
            var originalProbationEndDate = employee.ProbationEndDate
                ?? ((DateTime)employee.JoiningDate).AddMonths(employee.Designation?.ProbationPeriodMonths ?? 3);

            var entity = new ProbationConfirmation
            {
                Id = IDManager.GetNewId(new ProbationConfirmation()),

                EmployeeId = dto.EmployeeId,
                ProbationStartDate = (DateTime)employee.JoiningDate,
                OriginalProbationEndDate = originalProbationEndDate,

                Recommendation = recommendation,
                ExtendedProbationEndDate = recommendation == ProbationRecommendation.Extend
                    ? dto.ExtendedProbationEndDate
                    : null,

                MakerId = actingUserId,
                MakerActionOn = DateTime.UtcNow,
                MakerRemarks = string.IsNullOrWhiteSpace(dto.MakerRemarks) ? null : dto.MakerRemarks.Trim(),

                Status = ProbationConfirmationStatus.PendingChecker,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.ProbationConfirmations.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        private async Task<ProbationConfirmation> GetEntityByIdAsync(string id, string tenantId)
        {
            // FIX (defect M2): filter out soft-deleted records for
            // consistency with the rest of the codebase's soft-delete
            // convention (BaseEntity.IsDeleted).
            var entity = await _context.ProbationConfirmations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Probation Confirmation record not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (Create/
        // Approve/Reject) - no additional authorization check here.
        private async Task<ProbationConfirmationDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // Public "view a single record" path. Unlike WfhRequestService's
        // equivalent (own request / reporting manager / HR override), this
        // is HR-internal, not employee self-service - so it's simpler:
        // anyone holding View permission on this feature may view any
        // record tenant-wide, no per-record ownership restriction.
        public async Task<ProbationConfirmationDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Probation Confirmation records.");

            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        public async Task<List<ProbationConfirmationDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Probation Confirmation records.");

            var query = _context.ProbationConfirmations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProbationConfirmationStatus>(status, true, out var statusEnum))
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

        #endregion

        #region Maker-Checker Workflow

        public async Task<ProbationConfirmationDto> ApproveAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.ProbationConfirmations
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (record == null)
                throw new Exception("Probation Confirmation record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Checked BEFORE any permission check, with NO override (not
            // even for HR/Admin) - segregation of duties.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to approve Probation Confirmation records.");

            if (record.Status != ProbationConfirmationStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be approved.");

            record.Status = ProbationConfirmationStatus.Approved;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = string.IsNullOrWhiteSpace(dto?.CheckerRemarks) ? null : dto.CheckerRemarks.Trim();

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            var employee = record.Employee
                ?? await _context.Employees.FirstOrDefaultAsync(x => x.Id == record.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found.");

            // Apply the outcome to the Employee record per the Maker's
            // Recommendation.
            switch (record.Recommendation)
            {
                case ProbationRecommendation.Confirm:
                    employee.ConfirmationDate = DateTime.UtcNow.Date;

                    // Only auto-flip EmploymentType FROM Probation
                    // specifically - never assume every confirmation
                    // implies "Permanent" for a non-probationary
                    // EmploymentType.
                    if (employee.EmploymentType == EmploymentType.Probation)
                        employee.EmploymentType = EmploymentType.Permanent;

                    record.FinalConfirmationDate = employee.ConfirmationDate;
                    break;

                case ProbationRecommendation.Extend:
                    employee.ProbationEndDate = record.ExtendedProbationEndDate;
                    break;

                case ProbationRecommendation.PlaceOnPIP:
                    // HANDOFF, not implementation: intentionally no Employee
                    // change and no PIP record created here. The employee
                    // remains on probation. The (later) PIP module reads
                    // Approved ProbationConfirmation rows with
                    // Recommendation == PlaceOnPIP as its trigger to create
                    // its own PIP case.
                    break;

                case ProbationRecommendation.Terminate:
                    employee.RelievingDate = DateTime.UtcNow.Date;
                    break;
            }

            employee.ModifiedOn = DateTime.UtcNow;
            employee.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        public async Task<ProbationConfirmationDto> RejectAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.ProbationConfirmations
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (record == null)
                throw new Exception("Probation Confirmation record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Same ordering as ApproveAsync: identity check first, no
            // override.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to act on Probation Confirmation records.");

            if (record.Status != ProbationConfirmationStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be rejected.");

            if (string.IsNullOrWhiteSpace(dto?.CheckerRemarks))
                throw new Exception("Checker remarks are required to reject a Probation Confirmation.");

            record.Status = ProbationConfirmationStatus.Rejected;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = dto.CheckerRemarks.Trim();

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            // No changes applied to the Employee record - the maker may
            // submit a new proposal afterward by calling CreateAsync again,
            // no explicit "resubmit" action needed.
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        // THE CORE MAKER-CHECKER INVARIANT: the acting user's own resolved
        // identity (their User.Id, i.e. actingUserId - NOT their linked
        // EmployeeId, since HR staff acting as checker typically aren't
        // the subject Employee at all) must differ from record.MakerId.
        // There is no override for this - not even HR/Admin can check
        // their own maker action.
        private static void EnsureCheckerIsNotMaker(ProbationConfirmation record, string actingUserId)
        {
            if (!string.IsNullOrEmpty(actingUserId) && actingUserId == record.MakerId)
                throw new UnauthorizedAccessException(
                    "The checker must be a different person than the maker - you cannot approve your own submission.");
        }

        #endregion

        #region Authorization Helpers

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the
        // PROBATION_CONFIRMATION feature - same join shape as
        // AttendanceService.IsHrOrAdminForAttendanceAsync /
        // WfhRequestService.IsHrOrAdminForAttendanceAsync, but scoped to
        // this feature's own FeatureId and parameterized by action (View/
        // Create/Approve) rather than hard-coded to View, since this
        // service needs all three distinctly.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.PROBATION_CONFIRMATION && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        private static ProbationConfirmationDto AssembleDto(ProbationConfirmation x)
        {
            return new ProbationConfirmationDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                DesignationName = x.Employee?.Designation?.Name,

                ProbationStartDate = x.ProbationStartDate,
                OriginalProbationEndDate = x.OriginalProbationEndDate,

                Recommendation = (int)x.Recommendation,
                RecommendationName = x.Recommendation.ToString(),

                ExtendedProbationEndDate = x.ExtendedProbationEndDate,

                MakerId = x.MakerId,
                MakerActionOn = x.MakerActionOn,
                MakerRemarks = x.MakerRemarks,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                CheckerId = x.CheckerId,
                CheckerActionOn = x.CheckerActionOn,
                CheckerRemarks = x.CheckerRemarks,

                FinalConfirmationDate = x.FinalConfirmationDate,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        // MakerId/CheckerId store the acting User.Id - resolve a display
        // name via the linked Employee if one exists, else fall back to
        // the account's Username. Mirrors
        // WfhRequestService.BuildApprovedByNameMapAsync /
        // AttendanceRegularizationService's equivalent.
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

        private async Task<ProbationConfirmationDto> AssembleDtoAsync(ProbationConfirmation x)
        {
            var dto = AssembleDto(x);

            var nameMap = await BuildUserNameMapAsync(new[] { x.MakerId, x.CheckerId });

            dto.MakerName = nameMap.TryGetValue(x.MakerId, out var makerName) ? makerName : null;

            if (!string.IsNullOrEmpty(x.CheckerId))
                dto.CheckerName = nameMap.TryGetValue(x.CheckerId, out var checkerName) ? checkerName : null;

            return dto;
        }

        private async Task<List<ProbationConfirmationDto>> AssembleDtoListAsync(List<ProbationConfirmation> entities)
        {
            var userIds = entities.SelectMany(x => new[] { x.MakerId, x.CheckerId });
            var nameMap = await BuildUserNameMapAsync(userIds);

            var list = new List<ProbationConfirmationDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                dto.MakerName = nameMap.TryGetValue(x.MakerId, out var makerName) ? makerName : null;

                if (!string.IsNullOrEmpty(x.CheckerId) && nameMap.TryGetValue(x.CheckerId, out var checkerName))
                    dto.CheckerName = checkerName;

                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
