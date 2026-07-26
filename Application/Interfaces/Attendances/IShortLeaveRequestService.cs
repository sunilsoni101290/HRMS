using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    public interface IShortLeaveRequestService
    {
        Task<ShortLeaveRequestDto> CreateAsync(CreateShortLeaveRequestDto dto, string tenantId, string actingUserId);

        // Enforces view-authorization: only the request's own employee,
        // their direct Reporting Manager, or HR/Admin may fetch it.
        Task<ShortLeaveRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // Admin/HR view of every request in the tenant, with optional
        // status/department/free-text filters.
        Task<List<ShortLeaveRequestDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search);

        // "My requests" for the currently logged-in user - actingUserId is
        // their User.Id (resolved from the JWT "UserId" claim by the
        // controller); their linked EmployeeId is resolved internally, same
        // as IWfhRequestService.GetMyRequestsAsync.
        Task<List<ShortLeaveRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId);

        // Pending requests awaiting THIS acting user's action - all pending
        // tenant-wide if they are HR/Admin, otherwise only requests where
        // they are the requesting employee's direct Reporting Manager.
        Task<List<ShortLeaveRequestDto>> GetPendingForApproverAsync(string actingUserId, string tenantId);

        Task<ShortLeaveRequestDto> ApproveAsync(string id, string actingUserId, string tenantId);

        Task<ShortLeaveRequestDto> RejectAsync(string id, string reason, string actingUserId, string tenantId);

        Task<ShortLeaveRequestDto> CancelAsync(string id, string actingUserId, string tenantId);
    }
}
