using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveBalanceService
    {
        Task<List<LeaveBalanceDto>> GetAllAsync();

        Task<List<LeaveBalanceDto>> GetByEmployeeAsync(string employeeId);

        Task<LeaveBalanceDto?> GetByIdAsync(string id);

        Task<LeaveBalanceDto> CreateAsync(LeaveBalanceDto dto);

        Task<LeaveBalanceDto?> UpdateAsync(string id, LeaveBalanceDto dto);

        Task<bool> DeleteAsync(string id);

        Task<LeaveBalanceDto?> GetEmployeeLeaveBalanceAsync(
            string employeeId,
            string leaveTypeId,
            int year);

        Task<bool> AllocateLeaveAsync(
        string employeeId,
        int year);

        Task<bool> DeductLeaveAsync(
            string employeeId,
            string leaveTypeId,
            decimal days);

        Task<bool> CreditLeaveAsync(
            string employeeId,
            string leaveTypeId,
            decimal days);

        Task<bool> CarryForwardLeaveAsync(
            string employeeId,
            int fromYear,
            int toYear);
    }
}
