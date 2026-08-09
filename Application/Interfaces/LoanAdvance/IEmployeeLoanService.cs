using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Orchestrates the full EmployeeLoan lifecycle (Phase 2 state
    /// machine) - Maker-Checker submission/approval, Finance disbursement,
    /// pre-closure and settlement. Money-math (EMI schedule, eligibility,
    /// pre-closure quote) is delegated to <see cref="ILoanCalculationService"/>;
    /// this service owns state transitions, authorization, and persistence.
    /// </summary>
    public interface IEmployeeLoanService
    {
        Task<List<EmployeeLoanListDto>> GetAllAsync(string tenantId, string actingUserId, string? status = null, string? employeeId = null);
        Task<EmployeeLoanDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        /// <summary>Requests awaiting the acting user's action at their currently-resolved approval level.</summary>
        Task<List<EmployeeLoanListDto>> GetPendingOnMeAsync(string tenantId, string actingUserId);

        Task<LoanEligibilityDto> CheckEligibilityAsync(string employeeId, string loanTypeId, decimal requestedAmount, int tenureMonths, string tenantId, string actingUserId);

        /// <summary>
        /// Live, non-persisted EMI amortization preview shown on the
        /// request form as the employee adjusts amount/tenure - builds a
        /// throwaway EmployeeLoan (never saved) and runs it through
        /// ILoanCalculationService.GenerateEmiSchedule using the resolved
        /// LoanPolicy/LoanType interest rate.
        /// </summary>
        Task<EmiPreviewResponseDto> PreviewEmiAsync(EmiPreviewRequestDto dto, string tenantId, string actingUserId);

        /// <summary>MAKER action - creates the request in Submitted state and resolves/advances into the first approval level.</summary>
        Task<EmployeeLoanDto> SubmitAsync(LoanSubmitDto dto, string tenantId, string actingUserId);

        /// <summary>CHECKER action at the request's CurrentApprovalLevel. actingUserId must differ from MakerId and be a resolved approver (or delegate) for that level.</summary>
        Task<EmployeeLoanDto> ApproveAsync(LoanApprovalActionDto dto, string tenantId, string actingUserId);

        Task<EmployeeLoanDto> RejectAsync(LoanApprovalActionDto dto, string tenantId, string actingUserId);

        /// <summary>Finance action - Approved -> Disbursed, generates the EMI schedule via ILoanCalculationService.</summary>
        Task<EmployeeLoanDto> DisburseAsync(LoanDisbursementDto dto, string tenantId, string actingUserId);

        Task<LoanPreClosureQuoteDto> GetPreClosureQuoteAsync(string employeeLoanId, string tenantId, string actingUserId);

        /// <summary>Active -> PreClosureRequested.</summary>
        Task<EmployeeLoanDto> RequestPreClosureAsync(string employeeLoanId, string tenantId, string actingUserId);

        /// <summary>Confirms the lump-sum payment, cancels remaining Pending EMIs, moves the loan to Closed.</summary>
        Task<EmployeeLoanDto> SettleAsync(LoanSettlementDto dto, string tenantId, string actingUserId);

        /// <summary>Phase 15 - resolved approver/delegate User.Ids at a PendingApproval loan's CurrentApprovalLevel, for the background reminder sweep (LoanAdvanceReminderService). Empty if the loan isn't currently PendingApproval or the level has no resolvable approver.</summary>
        Task<List<string>> ResolveCurrentApproverUserIdsAsync(string employeeLoanId);
    }
}
