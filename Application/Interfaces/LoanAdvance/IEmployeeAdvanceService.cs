using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>Mirror of IEmployeeLoanService for EmployeeAdvance - no interest/EMI, flat installments, no pre-closure state.</summary>
    public interface IEmployeeAdvanceService
    {
        Task<List<EmployeeAdvanceListDto>> GetAllAsync(string tenantId, string actingUserId, string? status = null, string? employeeId = null);
        Task<EmployeeAdvanceDto> GetByIdAsync(string id, string tenantId, string actingUserId);
        Task<List<EmployeeAdvanceListDto>> GetPendingOnMeAsync(string tenantId, string actingUserId);

        Task<EmployeeAdvanceDto> SubmitAsync(AdvanceSubmitDto dto, string tenantId, string actingUserId);
        Task<EmployeeAdvanceDto> ApproveAsync(AdvanceApprovalActionDto dto, string tenantId, string actingUserId);
        Task<EmployeeAdvanceDto> RejectAsync(AdvanceApprovalActionDto dto, string tenantId, string actingUserId);
        Task<EmployeeAdvanceDto> DisburseAsync(AdvanceDisbursementDto dto, string tenantId, string actingUserId);

        /// <summary>Manual settlement entry for any remaining balance outside the normal payroll cycle.</summary>
        Task<EmployeeAdvanceDto> SettleAsync(AdvanceSettlementDto dto, string tenantId, string actingUserId);

        /// <summary>Phase 15 - the single approver (Reporting Manager) User.Id for a PendingApproval advance, for the background reminder sweep. Empty if not currently PendingApproval or no manager on file.</summary>
        Task<List<string>> ResolveCurrentApproverUserIdsAsync(string employeeAdvanceId);
    }
}
