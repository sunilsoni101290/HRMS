using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Phase 14 - read-only aggregation/reporting over the Loan &amp; Advance
    /// module. Deliberately separate from ILoanCalculationService (per-loan
    /// business math) and the individual EmployeeLoan/EmployeeAdvance
    /// services (lifecycle orchestration) - this service only ever reads,
    /// never mutates state, and is gated by
    /// AppFeatureConstants.LOAN_ADVANCE_DASHBOARD / LOAN_ADVANCE_REPORT
    /// (View action) rather than EMPLOYEE_LOAN/EMPLOYEE_ADVANCE.
    /// </summary>
    public interface ILoanReportService
    {
        Task<LoanAdvanceDashboardDto> GetDashboardAsync(string tenantId, string actingUserId);

        /// <summary>Employee-wise outstanding balance across both Loans and Advances - optionally scoped to one department.</summary>
        Task<List<OutstandingBalanceReportRowDto>> GetOutstandingBalanceReportAsync(string tenantId, string actingUserId, string? departmentId = null);

        /// <summary>Every LoanPaymentHistory + AdvancePaymentHistory row in a date range, for the Payment History report.</summary>
        Task<List<LoanAdvancePaymentReportRowDto>> GetPaymentHistoryReportAsync(string tenantId, string actingUserId, DateTime fromDate, DateTime toDate);
    }
}
