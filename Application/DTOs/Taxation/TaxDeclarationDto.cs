using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Taxation
{
    // Full read/response shape for a TaxDeclaration - see
    // Domain/Entities/TaxDeclaration.cs.
    public class TaxDeclarationDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }

        public string FinancialYearId { get; set; }
        public string? FinancialYearName { get; set; }

        public int Regime { get; set; }
        public string? RegimeName { get; set; }

        public decimal Section80C { get; set; }
        public decimal Section80CCD1B { get; set; }
        public decimal Section80D { get; set; }
        public decimal Section24B { get; set; }
        public decimal OtherDeductions { get; set; }

        public decimal AnnualRentPaid { get; set; }
        public bool IsMetroCity { get; set; }
        public string? LandlordPAN { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? SubmittedOn { get; set; }

        public string? VerifiedBy { get; set; }
        public string? VerifiedByName { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public string? VerifierRemarks { get; set; }

        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Create/update input - a Maker (the employee, or HR/Admin on their
    // behalf) may only edit while Status == Draft, enforced in
    // TaxDeclarationService.CreateOrUpdateAsync, not via attributes here.
    public class CreateTaxDeclarationDto
    {
        [Required]
        public string EmployeeId { get; set; }
        public string TenantId { get; set; }
        public string ActingUserId { get; set; }

        [Required(ErrorMessage = "Financial Year is required")]
        public string FinancialYearId { get; set; }

        [Required(ErrorMessage = "Regime is required")]
        public int Regime { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Section80C { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Section80CCD1B { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Section80D { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Section24B { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OtherDeductions { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AnnualRentPaid { get; set; }

        public bool IsMetroCity { get; set; }

        [MaxLength(1000)]
        public string? LandlordPAN { get; set; }
    }

    // Body for PUT .../{id}/verify and .../{id}/reject - VerifierRemarks
    // is optional for Verify, required for Reject (enforced server-side in
    // TaxDeclarationService.RejectAsync, same pattern as
    // CheckerActionDto.CheckerRemarks for Probation Confirmation).
    public class TaxDeclarationVerifyActionDto
    {
        [MaxLength(1000)]
        public string? VerifierRemarks { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }
}
