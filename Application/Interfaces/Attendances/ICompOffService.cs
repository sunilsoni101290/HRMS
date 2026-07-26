using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    public interface ICompOffService
    {
        // HR-facing list of PendingReview candidates, tenant-wide (Comp Off
        // review is inherently HR-only - employees never submit a request,
        // they just passively earn candidates via the detection job).
        Task<List<CompOffCandidateDto>> GetPendingReviewAsync(string tenantId, string? departmentId, string? search);

        // The currently logged-in employee's own Comp Off history (pending +
        // reviewed) for transparency - actingUserId is their User.Id
        // (resolved from the JWT "UserId" claim by the controller); their
        // linked EmployeeId is resolved internally, same as
        // IWfhRequestService.GetMyRequestsAsync.
        Task<List<CompOffCandidateDto>> GetMyCreditsAsync(string actingUserId, string tenantId);

        // Enforces view-authorization: only the candidate's own employee, or
        // HR/Admin, may fetch it (no reporting-manager angle - Comp Off
        // review is HR-only, not manager-approved).
        Task<CompOffCandidateDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // HR/Admin-only. Re-verifies Status is still PendingReview, credits
        // CreditedDays into the employee's "Comp Off" LeaveType balance for
        // the current year via ILeaveBalanceService.CreditLeaveAsync, then
        // sets Status = Approved.
        Task<CompOffCandidateDto> ApproveAsync(string id, string actingUserId, string tenantId);

        // HR/Admin-only. Sets Status = Rejected with a reason - no balance
        // change.
        Task<CompOffCandidateDto> RejectAsync(string id, string reason, string actingUserId, string tenantId);
    }
}
