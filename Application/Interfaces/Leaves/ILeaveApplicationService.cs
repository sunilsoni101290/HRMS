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
        Task<bool> CancelLeaveAsync(CancelLeaveRequestDto request);

        #endregion

        #region Queries
        Task<List<LeaveApplicationDto>>GetEmployeeLeavesAsync(string employeeId);
        Task<List<LeaveApplicationDto>>GetPendingLeavesAsync();
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
		
		
    }
}
