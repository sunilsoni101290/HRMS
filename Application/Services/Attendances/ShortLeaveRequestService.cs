using Application.DTOs.Attendances;
using Application.DTOs.Leaves;
using Application.Interfaces.Attendances;
using Application.Interfaces.Leaves;
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
    // Short Leave request - employee submits a request for a FEW HOURS off
    // during a working day (Date + FromTime/ToTime), approved single-level
    // by the requesting employee's direct Reporting Manager, or by HR/Admin
    // as a permission based override - same authorization pattern as
    // WfhRequestService/OnDutyRequestService (copied rather than shared,
    // matching how every module in this codebase hand-rolls its own
    // approval/permission helpers).
    //
    // Unlike WFH/On Duty, approving a Short Leave request does NOT write
    // back to Attendance (the employee is still physically present/working
    // most of the day). Instead, TotalHours is converted to a FRACTION of a
    // day via AttendancePolicy.ShortLeaveHoursPerDay and deducted from the
    // employee's chosen LeaveTypeId balance for the current year - a
    // sufficiency check runs at both Create (check-only) and Approve
    // (check + actually deduct), mirroring the check-at-submit/
    // deduct-at-approve convention already used by
    // LeaveApplicationService.ApplyLeaveAsync/ApproveLeaveAsync.
    public class ShortLeaveRequestService : IShortLeaveRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAttendancePolicyService _attendancePolicyService;
        private readonly ILeaveBalanceService _leaveBalanceService;

        // Fallback conversion factor when no active AttendancePolicy exists
        // for the tenant/company - never blocks a Short Leave request just
        // because nobody has configured a policy yet.
        private const decimal DefaultShortLeaveHoursPerDay = 8.0m;

        public ShortLeaveRequestService(
            ApplicationDbContext context,
            IAttendancePolicyService attendancePolicyService,
            ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _attendancePolicyService = attendancePolicyService;
            _leaveBalanceService = leaveBalanceService;
        }

        #region CRUD

        public async Task<ShortLeaveRequestDto> CreateAsync(CreateShortLeaveRequestDto dto, string tenantId, string actingUserId)
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
                    throw new UnauthorizedAccessException("You can only submit a Short Leave request for yourself.");
            }

            var leaveType = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == dto.LeaveTypeId && x.TenantId == tenantId && !x.IsDeleted);

            if (leaveType == null)
                throw new Exception("Leave type not found.");

            if (!leaveType.IsActive)
                throw new Exception("The selected leave type is not active.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new Exception("Reason is required.");

            if (dto.ToTime <= dto.FromTime)
                throw new Exception($"To Time ({dto.ToTime}) must be after From Time ({dto.FromTime}).");

            var totalHours = (decimal)(dto.ToTime - dto.FromTime).TotalHours;

            var hoursPerDay = await ResolveShortLeaveHoursPerDayAsync(tenantId, employee.CompanyId);

            var fractionalDays = Math.Round(totalHours / hoursPerDay, 3);

            // Check-only at submission - the balance is NOT deducted here,
            // only at ApproveAsync - see the class-level remarks above.
            await EnsureSufficientBalanceAsync(dto.EmployeeId, dto.LeaveTypeId, dto.Date.Year, fractionalDays);

            var entity = new ShortLeaveRequest
            {
                Id = IDManager.GetNewId(new ShortLeaveRequest()),

                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,

                Date = dto.Date.Date,
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                TotalHours = totalHours,
                FractionalDays = fractionalDays,

                Reason = dto.Reason.Trim(),

                Status = ShortLeaveRequestStatus.Pending,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.ShortLeaveRequests.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        private async Task<ShortLeaveRequest> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.ShortLeaveRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Short Leave request not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (Create/
        // Approve/Reject/Cancel) - no additional authorization check here,
        // since the acting user's right to perform that specific mutation
        // was already verified before this is called.
        private async Task<ShortLeaveRequestDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // Public "view a single request" path (API GET /{id}). Unlike the
        // internal re-fetch above, an arbitrary authenticated tenant user
        // could pass ANY request id here, so this enforces: only the
        // request's own employee, their direct Reporting Manager, or
        // HR/Admin may view it.
        public async Task<ShortLeaveRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwnRequest = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            if (!isOwnRequest && !isReportingManager && !await IsHrOrAdminForAttendanceAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this Short Leave request.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<ShortLeaveRequestDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search)
        {
            var query = _context.ShortLeaveRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.LeaveType)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ShortLeaveRequestStatus>(status, true, out var statusEnum))
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

        public async Task<List<ShortLeaveRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(employeeId))
                return new List<ShortLeaveRequestDto>();

            var entities = await _context.ShortLeaveRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<ShortLeaveRequestDto>> GetPendingForApproverAsync(string actingUserId, string tenantId)
        {
            bool isHrOrAdmin = await IsHrOrAdminForAttendanceAsync(actingUserId);
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            var query = _context.ShortLeaveRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.LeaveType)
                .Where(x => x.TenantId == tenantId && x.Status == ShortLeaveRequestStatus.Pending)
                .AsQueryable();

            if (!isHrOrAdmin)
            {
                if (string.IsNullOrEmpty(actingEmployeeId))
                    return new List<ShortLeaveRequestDto>();

                query = query.Where(x => x.Employee.ReportingManagerId == actingEmployeeId);
            }

            var entities = await query.OrderBy(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Workflow

        public async Task<ShortLeaveRequestDto> ApproveAsync(string id, string actingUserId, string tenantId)
        {
            var request = await _context.ShortLeaveRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Short Leave request not found.");

            if (request.Status != ShortLeaveRequestStatus.Pending)
                throw new Exception("Only pending requests can be approved.");

            await EnsureApproverAuthorizedAsync(request, actingUserId);

            // Re-check balance sufficiency at approval time too, in case
            // other deductions happened between submit and approve - mirrors
            // WfhRequestService's monthly-limit re-check at ApproveAsync.
            await EnsureSufficientBalanceAsync(request.EmployeeId, request.LeaveTypeId, request.Date.Year, request.FractionalDays);

            var deductRequest = new LeaveAdjustmentRequestDto
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Days = request.FractionalDays
            };

            bool deducted = await _leaveBalanceService.DeductLeaveAsync(deductRequest);

            if (!deducted)
                throw new Exception("Failed to deduct leave balance for this Short Leave request.");

            request.Status = ShortLeaveRequestStatus.Approved;
            request.ApprovedBy = actingUserId;
            request.ApprovedOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<ShortLeaveRequestDto> RejectAsync(string id, string reason, string actingUserId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection reason is required.");

            var request = await _context.ShortLeaveRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Short Leave request not found.");

            if (request.Status != ShortLeaveRequestStatus.Pending)
                throw new Exception("Only pending requests can be rejected.");

            await EnsureApproverAuthorizedAsync(request, actingUserId);

            // Nothing was deducted at submission - a Reject never needs to
            // credit anything back.
            request.Status = ShortLeaveRequestStatus.Rejected;
            request.RejectedBy = actingUserId;
            request.RejectedReason = reason.Trim();
            request.RejectedOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<ShortLeaveRequestDto> CancelAsync(string id, string actingUserId, string tenantId)
        {
            var request = await _context.ShortLeaveRequests
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Short Leave request not found.");

            // Only the request's own employee may cancel - resolve the
            // acting user's linked EmployeeId the same way as
            // ApproveAsync/RejectAsync's authorization check.
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(actingEmployeeId) || request.EmployeeId != actingEmployeeId)
                throw new UnauthorizedAccessException("You can only cancel your own Short Leave request.");

            if (request.Status != ShortLeaveRequestStatus.Pending)
                throw new Exception("Only pending requests can be cancelled.");

            // Nothing was deducted yet (Pending never touched the balance) -
            // no credit-back needed.
            request.Status = ShortLeaveRequestStatus.Cancelled;
            request.CancelledBy = actingUserId;
            request.CancelledOn = DateTime.UtcNow;

            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        #endregion

        #region Balance / Policy Helpers

        private async Task<decimal> ResolveShortLeaveHoursPerDayAsync(string tenantId, string? companyId)
        {
            var policyDto = await _attendancePolicyService.GetActiveForTenantAsync(tenantId, companyId);

            return policyDto != null && policyDto.ShortLeaveHoursPerDay > 0
                ? policyDto.ShortLeaveHoursPerDay
                : DefaultShortLeaveHoursPerDay;
        }

        private async Task EnsureSufficientBalanceAsync(string employeeId, string leaveTypeId, int year, decimal fractionalDays)
        {
            var balance = await _leaveBalanceService.GetEmployeeLeaveBalanceAsync(employeeId, leaveTypeId, year);

            var available = balance?.Balance ?? 0m;

            if (available < fractionalDays)
                throw new Exception(
                    $"Insufficient balance: {available} day(s) available, {fractionalDays} day(s) required.");
        }

        #endregion

        #region Authorization Helpers

        // Resolves the acting user's linked EmployeeId from their User.Id -
        // never trusted from client input - same pattern as
        // WfhRequestService.GetActingEmployeeIdAsync.
        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        // Same permission-based pattern as
        // WfhRequestService.IsHrOrAdminForAttendanceAsync (copied rather
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
        private async Task EnsureApproverAuthorizedAsync(ShortLeaveRequest request, string actingUserId)
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

            throw new UnauthorizedAccessException("You are not authorized to act on this Short Leave request.");
        }

        #endregion

        #region DTO Assembly

        private static ShortLeaveRequestDto AssembleDto(ShortLeaveRequest x)
        {
            return new ShortLeaveRequestDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                ReportingManagerName = x.Employee?.ReportingManager != null
                    ? $"{x.Employee.ReportingManager.FirstName} {x.Employee.ReportingManager.LastName}".Trim()
                    : null,

                LeaveTypeId = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType?.Name,

                Date = x.Date,
                FromTime = x.FromTime,
                ToTime = x.ToTime,
                TotalHours = x.TotalHours,
                FractionalDays = x.FractionalDays,

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
        // WfhRequestService.BuildApprovedByNameMapAsync.
        private async Task<Dictionary<string, string>> BuildApprovedByNameMapAsync(IEnumerable<ShortLeaveRequest> entities)
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

        private async Task<ShortLeaveRequestDto> AssembleDtoAsync(ShortLeaveRequest x)
        {
            var dto = AssembleDto(x);

            if (!string.IsNullOrEmpty(x.ApprovedBy))
            {
                var nameMap = await BuildApprovedByNameMapAsync(new[] { x });
                dto.ApprovedByName = nameMap.TryGetValue(x.ApprovedBy, out var name) ? name : null;
            }

            return dto;
        }

        private async Task<List<ShortLeaveRequestDto>> AssembleDtoListAsync(List<ShortLeaveRequest> entities)
        {
            var nameMap = await BuildApprovedByNameMapAsync(entities);

            var list = new List<ShortLeaveRequestDto>();

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
    }
}
