using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    public interface IOnDutyRequestService
    {
        Task<OnDutyRequestDto> CreateAsync(CreateOnDutyRequestDto dto, string tenantId, string actingUserId);

        // Enforces view-authorization: only the request's own employee,
        // their direct Reporting Manager, or HR/Admin may fetch it.
        Task<OnDutyRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // Admin/HR view of every request in the tenant, with optional
        // status/department/free-text filters.
        Task<List<OnDutyRequestDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search);

        // "My requests" for the currently logged-in user - actingUserId is
        // their User.Id (resolved from the JWT "UserId" claim by the
        // controller); their linked EmployeeId is resolved internally, the
        // same way GetPendingForApproverAsync/ApproveAsync do (mirrors
        // AttendanceService.GetTeamAttendanceAsync's controller/service
        // split - the controller never needs to already know the acting
        // user's EmployeeId).
        Task<List<OnDutyRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId);

        // Pending requests awaiting THIS acting user's action - all pending
        // tenant-wide if they are HR/Admin, otherwise only requests where
        // they are the requesting employee's direct Reporting Manager.
        Task<List<OnDutyRequestDto>> GetPendingForApproverAsync(string actingUserId, string tenantId);

        Task<OnDutyRequestDto> ApproveAsync(string id, string actingUserId, string tenantId);

        Task<OnDutyRequestDto> RejectAsync(string id, string reason, string actingUserId, string tenantId);

        Task<OnDutyRequestDto> CancelAsync(string id, string actingUserId, string tenantId);
    }
}
