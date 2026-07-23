using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
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

namespace Application.Services.Attendances
{
    // Work From Home request - employee submits a DATE RANGE
    // (FromDate..ToDate), approved single-level by the requesting
    // employee's direct Reporting Manager, or by HR/Admin as a permission
    // based override (see IsHrOrAdminForAttendanceAsync below - copied from
    // AttendanceService's identical check rather than shared, matching how
    // every module in this codebase hand-rolls its own approval/permission
    // helpers). On approval, an Attendance row is upserted for EVERY date
    // in the range with Status = AttendanceStatus.WorkFromHome - see
    // ApplyWfhToAttendanceAsync, which is the WFH analogue of
    // AttendanceRegularizationService.ApplyRegularizationToAttendanceAsync.
    public class WfhRequestService : IWfhRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAttendancePolicyService _attendancePolicyService;

        public WfhRequestService(ApplicationDbContext context, IAttendancePolicyService attendancePolicyService)
        {
            _context = context;
            _attendancePolicyService = attendancePolicyService;
        }

        #region CRUD

        public async Task<WfhRequestDto> CreateAsync(CreateWfhRequestDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            // A request can only be submitted for yourself, unless the
            // acting user is HR/Admin submitting on someone's behalf - never
            // trust dto.EmployeeId alone (it's client-supplied), same
            // reasoning as EnsureApproverAuthorizedAsync below.
            if (!await IsHrOrAdminForAttendanceAsync(actingUserId))
            {
                var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

                if (string.IsNullOrEmpty(actingEmployeeId) || actingEmployeeId != dto.EmployeeId)
                    throw new UnauthorizedAccessException("You can only submit a WFH request for yourself.");
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new Exception("Reason is required.");

            var fromDate = dto.FromDate.Date;
            var toDate = dto.ToDate.Date;

            if (fromDate > toDate)
                throw new Exception("From Date must be on or before To Date.");

            // No overlapping Pending/Approved WFH request already exists for
            // this employee covering any of the same dates.
            var conflict = await _context.WfhRequests
                .Where(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.TenantId == tenantId &&
                    (x.Status == WfhRequestStatus.Pending || x.Status == WfhRequestStatus.Approved) &&
                    x.FromDate.Date <= toDate && x.ToDate.Date >= fromDate)
                .FirstOrDefaultAsync();

            if (conflict != null)
                throw new Exception(
                    $"You already have a {conflict.Status} WFH request from {conflict.FromDate:dd-MMM-yyyy} to {conflict.ToDate:dd-MMM-yyyy} that overlaps these dates.");

            var totalDays = (toDate - fromDate).Days + 1;

            // Monthly limit check - skipped entirely if there's no active
            // policy, or the policy's MaxWfhDaysPerMonth is 0/unset
            // (unlimited).
            await ValidateMonthlyLimitAsync(employee.Id, tenantId, employee.CompanyId, fromDate, toDate);

            var entity = new WfhRequest
            {
                Id = IDManager.GetNewId(new WfhRequest()),

                EmployeeId = dto.EmployeeId,
                FromDate = fromDate,
                ToDate = toDate,
                TotalDays = totalDays,
                Reason = dto.Reason.Trim(),

                Status = WfhRequestStatus.Pending,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.WfhRequests.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        private async Task<WfhRequest> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.WfhRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("WFH request not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (Create/
        // Approve/Reject/Cancel) - no additional authorization check here,
        // since the acting user's right to perform that specific mutation
        // was already verified before this is called.
        private async Task<WfhRequestDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // Public "view a single request" path (API GET /{id}, MVC Details
        // page). Unlike the internal re-fetch above, an arbitrary
        // authenticated tenant user could pass ANY request id here, so this
        // enforces: only the request's own employee, their direct
        // Reporting Manager, or HR/Admin may view it.
        public async Task<WfhRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwnRequest = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            if (!isOwnRequest && !isReportingManager && !await IsHrOrAdminForAttendanceAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this WFH request.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<WfhRequestDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search)
        {
            var query = _context.WfhRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WfhRequestStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term) ||
                    x.Reason.Contains(term));
            }

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<WfhRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(employeeId))
                return new List<WfhRequestDto>();

            var entities = await _context.WfhRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<WfhRequestDto>> GetPendingForApproverAsync(string actingUserId, string tenantId)
        {
            bool isHrOrAdmin = await IsHrOrAdminForAttendanceAsync(actingUserId);
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            var query = _context.WfhRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Where(x => x.TenantId == tenantId && x.Status == WfhRequestStatus.Pending)
                .AsQueryable();

            if (!isHrOrAdmin)
            {
                if (string.IsNullOrEmpty(actingEmployeeId))
                    return new List<WfhRequestDto>();

                query = query.Where(x => x.Employee.ReportingManagerId == actingEmployeeId);
            }

            var entities = await query.OrderBy(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Workflow

        public async Task<WfhRequestDto> ApproveAsync(string id, string actingUserId, string tenantId)
        {
            var request = await _context.WfhRequests
                .Include(x => x.Employee).ThenInclude(e => e.DefaultShift)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("WFH request not found.");

            if (request.Status != WfhRequestStatus.Pending)
                throw new Exception("Only pending requests can be approved.");

            await EnsureApproverAuthorizedAsync(request, actingUserId);

            // Re-validate the monthly limit at approval time too, in case
            // other requests were approved in between create and approve.
            await ValidateMonthlyLimitAsync(
                request.EmployeeId, tenantId, request.Employee?.CompanyId,
                request.FromDate, request.ToDate, excludeRequestId: request.Id);

            request.Status = WfhRequestStatus.Approved;
            request.ApprovedBy = actingUserId;
            request.ApprovedOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await ApplyWfhToAttendanceAsync(request, actingUserId);

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<WfhRequestDto> RejectAsync(string id, string reason, string actingUserId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection reason is required.");

            var request = await _context.WfhRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("WFH request not found.");

            if (request.Status != WfhRequestStatus.Pending)
                throw new Exception("Only pending requests can be rejected.");

            await EnsureApproverAuthorizedAsync(request, actingUserId);

            request.Status = WfhRequestStatus.Rejected;
            request.RejectedBy = actingUserId;
            request.RejectedReason = reason.Trim();
            request.RejectedOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<WfhRequestDto> CancelAsync(string id, string actingUserId, string tenantId)
        {
            var request = await _context.WfhRequests
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("WFH request not found.");

            // Only the request's own employee may cancel - resolve the
            // acting user's linked EmployeeId the same way as
            // ApproveAsync/RejectAsync's authorization check.
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(actingEmployeeId) || request.EmployeeId != actingEmployeeId)
                throw new UnauthorizedAccessException("You can only cancel your own WFH request.");

            if (request.Status != WfhRequestStatus.Pending)
                throw new Exception("Only pending requests can be cancelled.");

            request.Status = WfhRequestStatus.Cancelled;
            request.CancelledBy = actingUserId;
            request.CancelledOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        #endregion

        #region Remaining Days

        public async Task<WfhRemainingDaysDto> GetRemainingWfhDaysAsync(string employeeId, int month, int year, string tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            var policyDto = await _attendancePolicyService.GetActiveForTenantAsync(tenantId, employee.CompanyId);

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var approvedRequests = await _context.WfhRequests
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.TenantId == tenantId &&
                    x.Status == WfhRequestStatus.Approved &&
                    x.FromDate.Date <= monthEnd && x.ToDate.Date >= monthStart)
                .ToListAsync();

            var usedDays = approvedRequests.Sum(r => OverlapDays(r.FromDate, r.ToDate, monthStart, monthEnd));

            int? max = policyDto != null && policyDto.MaxWfhDaysPerMonth > 0
                ? policyDto.MaxWfhDaysPerMonth
                : (int?)null;

            return new WfhRemainingDaysDto
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                MaxWfhDaysPerMonth = max,
                UsedDays = usedDays,
                RemainingDays = max.HasValue ? Math.Max(0, max.Value - usedDays) : (int?)null
            };
        }

        #endregion

        #region Monthly Limit Helpers

        // A request can span a month boundary - each calendar month the
        // range touches is checked separately against that month's
        // already-Approved WFH days for this employee.
        private async Task ValidateMonthlyLimitAsync(
            string employeeId, string tenantId, string? companyId,
            DateTime fromDate, DateTime toDate, string? excludeRequestId = null)
        {
            var policyDto = await _attendancePolicyService.GetActiveForTenantAsync(tenantId, companyId);

            if (policyDto == null || policyDto.MaxWfhDaysPerMonth <= 0)
                return; // No active policy, or unlimited - skip entirely.

            foreach (var monthStart in EnumerateTouchedMonths(fromDate, toDate))
            {
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var newDaysInMonth = OverlapDays(fromDate, toDate, monthStart, monthEnd);

                if (newDaysInMonth <= 0)
                    continue;

                var approvedInMonth = await _context.WfhRequests
                    .Where(x =>
                        x.EmployeeId == employeeId &&
                        x.TenantId == tenantId &&
                        x.Status == WfhRequestStatus.Approved &&
                        x.Id != excludeRequestId &&
                        x.FromDate.Date <= monthEnd && x.ToDate.Date >= monthStart)
                    .ToListAsync();

                var usedDays = approvedInMonth.Sum(r => OverlapDays(r.FromDate, r.ToDate, monthStart, monthEnd));

                if (usedDays + newDaysInMonth > policyDto.MaxWfhDaysPerMonth)
                    throw new Exception(
                        $"Monthly WFH limit exceeded for {monthStart:MMMM yyyy}: the policy allows {policyDto.MaxWfhDaysPerMonth} day(s)/month, " +
                        $"{usedDays} day(s) already approved, and this request needs {newDaysInMonth} more day(s) in that month.");
            }
        }

        private static IEnumerable<DateTime> EnumerateTouchedMonths(DateTime fromDate, DateTime toDate)
        {
            var cursor = new DateTime(fromDate.Year, fromDate.Month, 1);
            var last = new DateTime(toDate.Year, toDate.Month, 1);

            while (cursor <= last)
            {
                yield return cursor;
                cursor = cursor.AddMonths(1);
            }
        }

        private static int OverlapDays(DateTime aFrom, DateTime aTo, DateTime bFrom, DateTime bTo)
        {
            var start = aFrom.Date > bFrom.Date ? aFrom.Date : bFrom.Date;
            var end = aTo.Date < bTo.Date ? aTo.Date : bTo.Date;

            return start <= end ? (end - start).Days + 1 : 0;
        }

        #endregion

        #region Authorization Helpers

        // Resolves the acting user's linked EmployeeId from their User.Id -
        // never trusted from client input - same pattern as
        // AttendanceRegularizationService.GetActingContextAsync /
        // AttendanceService.GetTeamAttendanceAsync.
        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        // Same permission-based pattern as
        // AttendanceService.IsHrOrAdminForAttendanceAsync (copied rather
        // than shared - every module in this codebase hand-rolls its own) -
        // does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the View action on the ATTENDANCE
        // feature.
        private async Task<bool> IsHrOrAdminForAttendanceAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.ATTENDANCE && x.Action == Actions.View)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        // Either the acting user is the request's employee's direct
        // ReportingManagerId, or they hold the HR/Admin override.
        private async Task EnsureApproverAuthorizedAsync(WfhRequest request, string actingUserId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isReportingManager =
                !string.IsNullOrEmpty(request.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                request.Employee.ReportingManagerId == actingEmployeeId;

            if (isReportingManager)
                return;

            if (await IsHrOrAdminForAttendanceAsync(actingUserId))
                return;

            throw new UnauthorizedAccessException("You are not authorized to act on this WFH request.");
        }

        #endregion

        #region DTO Assembly

        private static WfhRequestDto AssembleDto(WfhRequest x)
        {
            return new WfhRequestDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                ReportingManagerName = x.Employee?.ReportingManager != null
                    ? $"{x.Employee.ReportingManager.FirstName} {x.Employee.ReportingManager.LastName}".Trim()
                    : null,

                FromDate = x.FromDate,
                ToDate = x.ToDate,
                TotalDays = x.TotalDays,

                Reason = x.Reason,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                ApprovedBy = x.ApprovedBy,
                ApprovedOn = x.ApprovedOn,

                RejectedReason = x.RejectedReason,
                RejectedOn = x.RejectedOn,

                CancelledOn = x.CancelledOn,

                Remarks = x.Remarks,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        // ApprovedBy stores the acting User.Id (not EmployeeId) - resolve a
        // display name via the linked Employee if one exists, else fall
        // back to the account's Username. Mirrors
        // AttendanceRegularizationService's BuildApprovedByNameMapAsync.
        private async Task<Dictionary<string, string>> BuildApprovedByNameMapAsync(IEnumerable<WfhRequest> entities)
        {
            var userIds = entities
                .Where(x => !string.IsNullOrEmpty(x.ApprovedBy))
                .Select(x => x.ApprovedBy!)
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<WfhRequestDto> AssembleDtoAsync(WfhRequest x)
        {
            var dto = AssembleDto(x);

            if (!string.IsNullOrEmpty(x.ApprovedBy))
            {
                var nameMap = await BuildApprovedByNameMapAsync(new[] { x });
                dto.ApprovedByName = nameMap.TryGetValue(x.ApprovedBy, out var name) ? name : null;
            }

            return dto;
        }

        private async Task<List<WfhRequestDto>> AssembleDtoListAsync(List<WfhRequest> entities)
        {
            var nameMap = await BuildApprovedByNameMapAsync(entities);

            var list = new List<WfhRequestDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                if (!string.IsNullOrEmpty(x.ApprovedBy) && nameMap.TryGetValue(x.ApprovedBy, out var name))
                    dto.ApprovedByName = name;

                list.Add(dto);
            }

            return list;
        }

        #endregion

        #region Attendance Write-Back

        // Final approval write-back: finds-or-creates the Attendance row
        // for EVERY date in FromDate..ToDate (a range, unlike Attendance
        // Regularization's single date) and sets
        // Status = AttendanceStatus.WorkFromHome. There is no punch data
        // for a WFH day (no First-In/Last-Out captured), so unlike
        // AttendanceRegularizationService.ApplyRegularizationToAttendanceAsync
        // (which recomputes hours FROM requested punch times), this credits
        // a full working day using the shift's configured FullDayMinutes
        // when the row has no existing punches - if a row already has
        // FirstIn/LastOut (e.g. captured separately via the mobile app
        // while working remotely), those hours are left untouched and only
        // Status is overwritten.
        private async Task ApplyWfhToAttendanceAsync(WfhRequest request, string approvedBy)
        {
            var employee = request.Employee ??
                await _context.Employees
                    .Include(x => x.DefaultShift)
                    .FirstOrDefaultAsync(x => x.Id == request.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found.");

            // Statuses that represent a day the employee wasn't expected to
            // work in the first place - approving a WFH request must never
            // silently convert an already-approved Leave, a Holiday, or a
            // WeekOff into a working WorkFromHome day (that would corrupt
            // attendance-% and payroll calculations for that day).
            var nonWorkingStatuses = new[]
            {
                AttendanceStatus.WeekOff, AttendanceStatus.Holiday, AttendanceStatus.Leave,
                AttendanceStatus.PaidLeave, AttendanceStatus.UnpaidLeave
            };

            for (var date = request.FromDate.Date; date <= request.ToDate.Date; date = date.AddDays(1))
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.TenantId == employee.TenantId && x.Date.Date == date);

                if (attendance != null && nonWorkingStatuses.Contains(attendance.Status))
                    continue; // leave Leave/Holiday/WeekOff days untouched

                Shift? shift;

                if (attendance != null)
                {
                    shift = !string.IsNullOrEmpty(attendance.ShiftId)
                        ? await _context.Shifts.FirstOrDefaultAsync(x => x.Id == attendance.ShiftId)
                        : null;
                }
                else
                {
                    shift = await ResolveShiftForDateAsync(employee, date);

                    attendance = new Attendance
                    {
                        Id = IDManager.GetNewId(new Attendance()),

                        TenantId = employee.TenantId,
                        CompanyId = employee.CompanyId,
                        BranchId = employee.BranchId,

                        EmployeeId = employee.Id,
                        ShiftId = shift?.Id,

                        Date = date,

                        CreatedBy = approvedBy,
                        CreatedOn = DateTime.UtcNow
                    };

                    _context.Attendances.Add(attendance);
                }

                attendance.Status = AttendanceStatus.WorkFromHome;
                attendance.IsManualEntry = true;

                // Only credit a full WFH day from the shift's configured
                // hours if this row has no punch data of its own - never
                // overwrite hours that were separately captured.
                if (!attendance.FirstIn.HasValue && !attendance.LastOut.HasValue)
                {
                    attendance.TotalWorkingHours = shift != null ? Math.Round(shift.FullDayMinutes / 60m, 2) : 0;
                    attendance.IsLate = false;
                    attendance.IsEarlyExit = false;
                    attendance.OvertimeHours = 0;
                }

                attendance.ProcessedOn = DateTime.UtcNow;
                attendance.ProcessedBy = approvedBy;

                attendance.ModifiedOn = DateTime.UtcNow;
                attendance.ModifiedBy = approvedBy;
            }
        }

        // Resolves the Shift that applies to the employee on the given date
        // - EmployeeShiftMapping (temporary/roster shift) takes priority
        // over the employee's DefaultShift - same as
        // AttendanceRegularizationService.ResolveShiftForDateAsync /
        // AttendanceService.PunchInAsync/PunchOutAsync.
        private async Task<Shift?> ResolveShiftForDateAsync(Employee employee, DateTime date)
        {
            var shiftMapping = await _context.EmployeeShiftMappings
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employee.Id &&
                    x.EffectiveFrom.Date <= date.Date &&
                    (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= date.Date));

            if (shiftMapping != null)
                return shiftMapping.Shift;

            if (employee.DefaultShift != null)
                return employee.DefaultShift;

            if (!string.IsNullOrEmpty(employee.ShiftId))
                return await _context.Shifts.FirstOrDefaultAsync(x => x.Id == employee.ShiftId);

            return null;
        }

        #endregion
    }
}
