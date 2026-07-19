using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveApplicationService
    {
        #region CRUD
        Task<LeaveApplicationDto> CreateAsync(ApplyLeaveRequestDto request);
        Task<LeaveApplicationDto> UpdateAsync(string id,ApplyLeaveRequestDto request);
        Task<bool> DeleteAsync(string id);
        Task<LeaveApplicationDto> GetByIdAsync(string id);
        Task<List<LeaveApplicationDto>> GetAllAsync();

        #endregion

        #region Workflow
        Task<bool> ApplyLeaveAsync(ApplyLeaveRequestDto request);
        Task<bool> ApproveLeaveAsync(ApproveLeaveRequestDto request);
        Task<bool> RejectLeaveAsync(RejectLeaveRequestDto request);
        Task<bool> SendBackLeaveAsync(SendBackLeaveRequestDto request);
        Task<LeaveApplicationDto> ResubmitAsync(string id, ApplyLeaveRequestDto request, string resubmittedBy);
        Task<bool> CancelLeaveAsync(CancelLeaveRequestDto request);

        #endregion

        #region Queries
        Task<List<LeaveApplicationDto>>GetEmployeeLeavesAsync(string employeeId);
        Task<List<LeaveApplicationDto>>GetPendingLeavesAsync();

        // Leave requests currently awaiting action from this specific
        // approver - Level 1/2 requests where they are the resolved
        // Reporting Manager/Department Head (or that person's currently
        // active out-of-office delegate - see IApprovalDelegationService),
        // plus every Level 3 request if userId actually holds the "Approve
        // Leave" permission (see IsHrApproverAsync).
        Task<List<LeaveApplicationDto>> GetPendingForApproverAsync(string? employeeId, string? userId);
        Task<List<LeaveApplicationDto>>GetApprovedLeavesAsync();
        Task<List<LeaveApplicationDto>>GetRejectedLeavesAsync();
        Task<List<LeaveApplicationDto>>GetCancelledLeavesAsync();
        Task<List<LeaveApplicationDto>>GetFilteredAsync(LeaveApplicationFilterRequestDto request);
        #endregion

        #region Count
        Task<int> GetPendingLeaveCountAsync();
        Task<int> GetApprovedLeaveCountAsync();
        Task<int> GetTodayLeaveCountAsync();
        #endregion

        #region Leave Approval History

        Task<List<LeaveApprovalHistoryDetailDto>>GetApprovalHistoryAsync(string leaveApplicationId);
        Task<List<LeaveApprovalHistoryDetailDto>>GetAllApprovalHistoryAsync();
        Task<LeaveApprovalHistoryDetailDto>GetApprovalHistoryByIdAsync(string id);

        #endregion

        #region Day Calculation

        // Counts only actual working days between fromDate and toDate -
        // week-offs (e.g. Sat/Sun) and holidays configured for the tenant
        // don't count against the employee's leave.
        Task<decimal> CalculateTotalDaysAsync(DateTime fromDate, DateTime toDate, bool isHalfDay, string? tenantId);

        #endregion

        #region Calendar

        // Approved leaves overlapping the given month - org-wide when
        // isAdmin (optionally further filtered to one department), or
        // scoped to the calling employee's own department ("their team")
        // when a self-service employee is asking, so they can plan around
        // colleagues without seeing every other department's leave.
        Task<LeaveCalendarResponseDto> GetCalendarAsync(
            int year,
            int month,
            string? employeeId,
            bool isAdmin,
            string? tenantId,
            string? departmentId = null);

        #endregion

        #region Authorization / Escalation Helpers

        // Real permission check backing Level 3 (HR) authorization - does
        // this User.Id hold, via any assigned Role, an allowed
        // RolePermission for the "Approve Leave" Permission (FeatureId =
        // LEAVE_APPROVAL, Action = Approve). Exposed publicly so both the
        // APP self-service controller (button-visibility only - the API is
        // still the real enforcement point) and other services can ask the
        // same question the workflow itself uses.
        Task<bool> IsHrApproverAsync(string? actingUserId);

        // Who should currently be notified/act on this pending leave -
        // delegate-aware for Level 1/2, fanned out to every HR-permission
        // holder for Level 3. Used by LeaveEscalationService
        // (API/BackgroundServices/LeaveEscalationService.cs) so its
        // stale-pending reminder targets the exact same approver(s) the live
        // workflow notifications already target.
        Task<List<string>> ResolveCurrentApproverUserIdsAsync(string leaveApplicationId);

        // Whoever is next in the chain after the current level - read-only,
        // used only to widen notification visibility for a long-overdue
        // request (see LeaveEscalationService). Never changes CurrentLevel.
        Task<List<string>> ResolveNextLevelApproverUserIdsAsync(string leaveApplicationId);

        #endregion
    }
}
