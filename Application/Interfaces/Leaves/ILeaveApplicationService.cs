using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveApplicationService
    {
        Task<List<LeaveApplicationDto>> GetAllAsync();

        Task<LeaveApplicationDto?> GetByIdAsync(string id);

        Task<List<LeaveApplicationDto>> GetByEmployeeAsync(string employeeId);

        Task<LeaveApplicationDto> ApplyLeaveAsync(LeaveApplicationDto dto);

        Task<bool> ApproveLeaveAsync(
            string leaveApplicationId,
            string approvedBy);

        Task<bool> RejectLeaveAsync(
            string leaveApplicationId,
            string approvedBy,
            string rejectionReason);

        Task<bool> CancelLeaveAsync(string leaveApplicationId);

        Task<bool> DeleteAsync(string id);

        Task<List<LeaveApplicationDto>> GetPendingApprovalsAsync();

        Task<List<LeaveApplicationDto>> GetApprovedLeavesAsync();

        Task<List<LeaveApplicationDto>> GetRejectedLeavesAsync();

        Task<List<LeaveApplicationDto>> GetByDateRangeAsync(
            DateTime fromDate,
            DateTime toDate);

        Task<int> GetPendingLeaveCountAsync();

        Task<int> GetApprovedLeaveCountAsync();

        Task<int> GetTodayLeaveCountAsync();
    }
}
