using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // The computed result of running TaxComputationService.ComputeAsync
    // for one (EmployeeId, FinancialYearId) - one row per employee per
    // Financial Year, recomputed (overwritten in place) whenever the
    // employee's Verified TaxDeclaration or projected annual salary
    // changes and Recompute is triggered again. This is the record Payroll
    // (and, later, Form-16) read from - MonthlyTdsForRemainingMonths is
    // the figure a payroll run should deduct each month going forward.
    public class EmployeeTaxComputation : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public string FinancialYearId { get; set; }
        public virtual FinancialYear FinancialYear { get; set; }

        // Null if computed with no Verified declaration on file (defaults
        // applied - New Regime, zero Chapter VI-A deductions, per Income
        // Tax Act's own default-regime rule).
        public string? TaxDeclarationId { get; set; }
        public virtual TaxDeclaration? TaxDeclaration { get; set; }

        public TaxRegime Regime { get; set; }

        // Projected full-year gross salary used as the basis for this
        // computation - see
        // TaxComputationService.ProjectAnnualGrossSalaryAsync.
        public decimal AnnualGrossSalary { get; set; }

        public decimal StandardDeduction { get; set; }

        // 0 under New Regime (HRA exemption is an Old Regime-only
        // concept).
        public decimal HraExemption { get; set; }

        // Sum of (capped) 80C + 80CCD(1B) + 80D + 24(b) + OtherDeductions -
        // 0 under New Regime.
        public decimal TotalChapterVIADeductions { get; set; }

        public decimal TaxableIncome { get; set; }

        public decimal TaxBeforeCess { get; set; }

        // Section 87A rebate - full rebate (tax reduced to zero, subject
        // to a rebate cap) when taxable income is at/under the
        // regime-specific threshold - see
        // TaxComputationService.ApplyRebate87A.
        public decimal Rebate87A { get; set; }

        // 4% Health & Education Cess on (TaxBeforeCess - Rebate87A).
        public decimal HealthEducationCess { get; set; }

        public decimal AnnualTaxLiability { get; set; }

        // Sum of TDS already withheld this FY via prior payroll runs -
        // read from Payroll/PayrollDetail rows tagged against a "TDS"/
        // "Income Tax" SalaryComponent, so a mid-year computation doesn't
        // double-deduct what's already been withheld.
        public decimal TdsDeductedTillDate { get; set; }

        // (AnnualTaxLiability - TdsDeductedTillDate) spread evenly over
        // the remaining payroll months in the Financial Year - the figure
        // a payroll run should deduct going forward. Never negative
        // (floored at 0 - see TaxComputationService).
        public decimal MonthlyTdsForRemainingMonths { get; set; }

        public DateTime ComputedOn { get; set; }
        public string? ComputedBy { get; set; }

        public override string GetSequencePrefix() => "ETC";
    }
}
