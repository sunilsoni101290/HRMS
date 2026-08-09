using Application.DTOs.LoanAdvance;
using Domain.Entities;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Pure, side-effect-free money-math for the Loan module - kept
    /// separate from EmployeeLoanService (which owns state
    /// transitions/persistence) so it's unit-testable without a database
    /// (see Phase 17) and so Phase 8 can flesh out
    /// CheckEligibilityAsync/GetPreClosureQuoteAsync fully without
    /// touching the workflow orchestration in EmployeeLoanService at all.
    ///
    /// PHASE 6 STATUS: GenerateEmiScheduleAsync has a real, correct
    /// reducing-balance/flat implementation (EmployeeLoanService.DisburseAsync
    /// depends on it structurally, so it can't be a stub). CheckEligibilityAsync
    /// and GetPreClosureQuoteAsync are intentionally minimal placeholders
    /// here - Phase 8 ("Business Logic: Eligibility, Interest, EMI, Payroll
    /// Recovery, Settlement") is where they get their full logic (net-salary
    /// lookups, existing-deduction aggregation, accrued-interest-to-date
    /// math). See the XML remarks on each method below.
    /// </summary>
    public interface ILoanCalculationService
    {
        /// <summary>
        /// Builds the full installment-by-installment amortization
        /// schedule for a just-Disbursed loan, per loan.InterestMethod.
        /// Does not persist - EmployeeLoanService.DisburseAsync adds the
        /// returned entities to the LoanEmiSchedule repository itself.
        /// </summary>
        List<LoanEmiSchedule> GenerateEmiSchedule(EmployeeLoan loan, DateTime firstDueDate);

        /// <summary>
        /// PHASE 8 TODO: full eligibility check (service tenure, existing
        /// active loans vs LoanPolicy.MaxActiveLoans, net-salary-based
        /// MaxDeductionPercentOfNetSalary headroom against ALL existing
        /// EMI/installment deductions, not just this LoanType). Phase 6's
        /// EmployeeLoanService.SubmitAsync calls this and surfaces
        /// whatever it returns - it does not duplicate eligibility logic
        /// itself.
        /// </summary>
        Task<LoanEligibilityDto> CheckEligibilityAsync(string employeeId, string loanTypeId, decimal requestedAmount, int tenureMonths, string tenantId);

        /// <summary>
        /// PHASE 8 TODO: full accrued-interest-to-date + policy pre-closure
        /// penalty calculation. Phase 6 wires the Pre-Closure workflow
        /// state transitions (see EmployeeLoanService.RequestPreClosureAsync/
        /// SettleAsync) but delegates the actual quote number to this method.
        /// </summary>
        Task<LoanPreClosureQuoteDto> GetPreClosureQuoteAsync(string employeeLoanId, DateTime asOfDate, string tenantId);
    }
}
