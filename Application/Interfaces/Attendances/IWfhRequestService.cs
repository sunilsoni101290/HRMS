using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    public interface IWfhRequestService
    {
        Task<WfhRequestDto> CreateAsync(CreateWfhRequestDto dto, string tenantId, string actingUserId);

        // Enforces view-authorization: only the request's own employee,
        // their direct Reporting Manager, or HR/Admin may fetch it.
        Task<WfhRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // Admin/HR view of every request in the tenant, with optional
        // status/department/free-text filters.
        Task<List<WfhRequestDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search);

        // "My requests" for the currently logged-in user - actingUserId is
        // their User.Id (resolved from the JWT "UserId" claim by the
        // controller); their linked EmployeeId is resolved internally, the
        // same way GetPendingForApproverAsync/ApproveAsync do (mirrors
        // AttendanceService.GetTeamAttendanceAsync's controller/service
        // split - the controller never needs to already know the acting
        // user's EmployeeId).
        Task<List<WfhRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId);

        // Pending requests awaiting THIS acting user's action - all pending
        // tenant-wide if they are HR/Admin, otherwise only requests where
        // they are the requesting employee's direct Reporting Manager.
        Task<List<WfhRequestDto>> GetPendingForApproverAsync(string actingUserId, string tenantId);

        Task<WfhRequestDto> ApproveAsync(string id, string actingUserId, string tenantId);

        Task<WfhRequestDto> RejectAsync(string id, string reason, string actingUserId, string tenantId);

        Task<WfhRequestDto> CancelAsync(string id, string actingUserId, string tenantId);

        // Policy limit / already-used / remaining WFH days for this
        // employee for the given calendar month - drives the "X of Y WFH
        // days used this month" hint on the create form.
        Task<WfhRemainingDaysDto> GetRemainingWfhDaysAsync(string employeeId, int month, int year, string tenantId);
    }
}
