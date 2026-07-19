using Application.DTOs.Leaves;

namespace Application.Interfaces.Leaves
{
    // Out-of-office proxy approver: lets a Reporting Manager or Department
    // Head (Level 1/2 of the Leave Application approval chain) designate a
    // delegate for a date range, so leave requests awaiting their approval
    // during that window are also authorized for the delegate. See
    // Domain/Entities/ApprovalDelegation.cs and
    // LeaveApplicationService.IsAuthorizedForLevelAsync (the consumer of
    // GetActiveDelegateForAsync below).
    public interface IApprovalDelegationService
    {
        Task<ApprovalDelegationDto> CreateAsync(CreateApprovalDelegationRequestDto request);

        // Manual early revocation - flips IsActive to false rather than
        // deleting the row, so the audit trail survives. revokedBy is
        // checked against the delegation's own DelegatorEmployeeId (via the
        // caller's linked employee) by the controller, not here - this
        // service trusts whatever employeeId scoping the caller has already
        // enforced, same division of responsibility as
        // LeaveApplicationService.CancelLeaveAsync's callers.
        Task<bool> RevokeAsync(string id, string? revokedBy);

        // Delegations this employee created (as the delegator) - drives the
        // "My Delegations" list view.
        Task<List<ApprovalDelegationDto>> GetForEmployeeAsync(string employeeId);

        // The employeeId currently authorized to act on delegatorEmployeeId's
        // behalf on the given date (an active, non-revoked delegation whose
        // [StartDate, EndDate] window covers that date), or null if none.
        // Callers checking "is this action allowed right now" should always
        // pass DateTime.UtcNow/today - the delegation being asked about is
        // "is a proxy authorized today", never the leave application's own
        // FromDate/ToDate.
        Task<string?> GetActiveDelegateForAsync(string? delegatorEmployeeId, DateTime onDate);
    }
}
