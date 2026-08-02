using Application.DTOs.Taxation;

namespace Application.Interfaces.Taxation
{
    // The core Income Tax / TDS calculation engine - see
    // Domain/Entities/EmployeeTaxComputation.cs / TaxComputationService.
    // HR/Payroll-only (Approve permission on TAX_DECLARATION doubles as
    // the "may trigger/view computations" gate - there is no separate
    // TAX_COMPUTATION permission check inside the service itself; the
    // TAX_COMPUTATION feature constant exists for menu/routing purposes
    // on the APP side only). Employees see their own numbers via
    // TaxDeclarationService.GetByIdAsync -> Details view, not through
    // this interface directly.
    public interface ITaxComputationService
    {
        // Runs (or re-runs) the full computation for one employee/FY -
        // projects annual gross salary from the latest SalaryStructure,
        // reads the Verified TaxDeclaration (if any; defaults to New
        // Regime with zero deductions if none exists, per the Income Tax
        // Act's own default-regime rule), applies slabs from TaxSlab,
        // Section 87A rebate, and 4% cess, then nets off TDS already
        // deducted this FY to produce MonthlyTdsForRemainingMonths.
        // Upserts (one row per EmployeeId+FinancialYearId).
        Task<EmployeeTaxComputationDto> ComputeAsync(string employeeId, string financialYearId, string tenantId, string actingUserId);

        Task<EmployeeTaxComputationDto?> GetAsync(string employeeId, string financialYearId, string tenantId, string actingUserId);

        Task<List<EmployeeTaxComputationDto>> GetAllAsync(string tenantId, string financialYearId, string? departmentId, string? search, string actingUserId);

        // Integration point for Payroll: the TDS amount to deduct for
        // this employee this month, given the most recent computation for
        // the Financial Year covering payPeriod. Returns 0 if no
        // computation exists yet (Payroll must not fail/block just
        // because Taxation hasn't been set up for an employee) - callers
        // needing to distinguish "0 because genuinely no tax due" from
        // "0 because not computed yet" should call GetAsync first.
        Task<decimal> GetMonthlyTdsAsync(string employeeId, DateTime payPeriod, string tenantId);
    }
}
