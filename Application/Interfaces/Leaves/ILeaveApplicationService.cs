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
        // Reporting Manager/Department Head, plus every Level 3 request if
        // their role name contains "HR".
        Task<List<LeaveApplicationDto>> GetPendingForApproverAsync(string? employeeId, string? roleName);
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
		
		
    }
}
