using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveBalanceService
    {
        #region Leave Balance

        Task<List<LeaveBalanceDto>> GetAllAsync();

        Task<LeaveBalanceDto?> GetByIdAsync(string id);

        Task<List<LeaveBalanceDto>> GetByEmployeeAsync(string employeeId);

        Task<LeaveBalanceDto?> GetEmployeeLeaveBalanceAsync(string employeeId,string leaveTypeId,int year);

        Task<LeaveBalanceDto> CreateAsync(LeaveBalanceDto dto);

        Task<LeaveBalanceDto?> UpdateAsync(string id,LeaveBalanceDto dto);

        Task<bool> DeleteAsync(string id);

        #endregion

        #region Leave Operations
        Task<bool> AllocateLeaveAsync(AllocateLeaveRequestDto request);
        Task<bool> CreditLeaveAsync(LeaveAdjustmentRequestDto request);
        Task<bool> DeductLeaveAsync(LeaveAdjustmentRequestDto request);
        Task<bool> CarryForwardLeaveAsync(CarryForwardLeaveRequestDto request);
        #endregion

        #region Transactions

        Task<List<LeaveBalanceTransactionDto>> GetTransactionsAsync(
            LeaveTransactionFilterRequestDto request);

        Task<List<LeaveBalanceTransactionDto>> GetEmployeeTransactionsAsync(
            EmployeeTransactionRequestDto request);

        Task<List<LeaveBalanceTransactionDto>> GetTransactionsByDateRangeAsync(
            TransactionDateRangeRequestDto request);

        #endregion
    }
}
