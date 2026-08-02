using System;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Taxation.EmployeeTaxComputationDto exactly.
    // Backed by API/Controllers/TaxComputationController.cs
    // (api/taxcomputation).
    public class EmployeeTaxComputationDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? PAN { get; set; }

        public string FinancialYearId { get; set; }
        public string? FinancialYearName { get; set; }

        public string? TaxDeclarationId { get; set; }

        public int Regime { get; set; }
        public string? RegimeName { get; set; }

        public decimal AnnualGrossSalary { get; set; }
        public decimal StandardDeduction { get; set; }
        public decimal HraExemption { get; set; }
        public decimal TotalChapterVIADeductions { get; set; }
        public decimal TaxableIncome { get; set; }

        public decimal TaxBeforeCess { get; set; }
        public decimal Rebate87A { get; set; }
        public decimal HealthEducationCess { get; set; }
        public decimal AnnualTaxLiability { get; set; }

        public decimal TdsDeductedTillDate { get; set; }
        public decimal MonthlyTdsForRemainingMonths { get; set; }

        public DateTime ComputedOn { get; set; }
        public string? ComputedBy { get; set; }
        public string? ComputedByName { get; set; }
    }
}
