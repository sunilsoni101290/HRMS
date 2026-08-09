using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Phase 8 payroll-recovery batch engine - hooked into
    /// PayrollBusinessService.GenerateAsync (see the call site there) so
    /// every payroll run also deducts that period's due
    /// LoanEmiSchedule/AdvanceInstallment rows, updates the running
    /// EmployeeLoan.OutstandingPrincipal / EmployeeAdvance.OutstandingAmount,
    /// and auto-closes an account once its balance reaches zero.
    ///
    /// IDEMPOTENCY (per Phase 2 §2.4 "Payroll Recovery Flow"): the guard is
    /// simply InstallmentStatus.Pending - a row that RecoverForPayrollAsync
    /// already recovered is stamped Recovered/Skipped with PayrollId set,
    /// so calling this method again for the same PayrollId (a re-run of a
    /// still-Draft payroll, or a manual admin retry) finds nothing left to
    /// do and is a safe no-op. It is intentionally NOT scoped to only the
    /// module's own IUnitOfWork transaction - each employee's recovery is
    /// committed independently so one employee's failure never blocks or
    /// rolls back another's, or the payroll generation itself.
    /// </summary>
    public interface IPayrollLoanRecoveryService
    {
        /// <summary>
        /// Recovers all due, still-Pending loan EMI and advance
        /// installments for the employee behind the given Payroll record,
        /// capped so the employee's net pay is never driven negative.
        /// Safe to call more than once for the same payrollId.
        /// </summary>
        Task<PayrollRecoveryResultDto> RecoverForPayrollAsync(string payrollId, string tenantId, string actingUserId);
    }
}
