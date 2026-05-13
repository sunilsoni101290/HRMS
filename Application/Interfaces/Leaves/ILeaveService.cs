using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveService
    {
        Task<LeaveResponseDto> ApplyLeaveAsync(LeaveApplyDto dto);
        Task<LeaveResponseDto> ApproveLeaveAsync(LeaveApproveDto dto);
        Task<LeaveResponseDto> RejectLeaveAsync(LeaveRejectDto dto);
        Task<List<LeaveResponseDto>> GetEmployeeLeaves(string employeeId);
        Task<LeaveResponseDto> CancelLeaveAsync(LeaveCancelDto dto);
    }
}
