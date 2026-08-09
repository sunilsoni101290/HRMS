using System;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Taxation.TaxDeclarationDto exactly. Backed
    // by API/Controllers/TaxDeclarationController.cs (api/taxdeclaration).
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
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Mirrors Application.DTOs.Taxation.CreateTaxDeclarationDto exactly -
    // the Create/Edit view's form-bound model.
    public class CreateTaxDeclarationDto
    {
        [Required]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Financial Year is required")]
        [Display(Name = "Financial Year")]
        public string FinancialYearId { get; set; }

        [Required(ErrorMessage = "Regime is required")]
        [Display(Name = "Tax Regime")]
        public int Regime { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Section 80C (LIC/PPF/ELSS/EPF etc.)")]
        public decimal Section80C { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Section 80CCD(1B) - Additional NPS")]
        public decimal Section80CCD1B { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Section 80D - Medical Insurance")]
        public decimal Section80D { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Section 24(b) - Home Loan Interest")]
        public decimal Section24B { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Other Deductions (80E/80G/80TTA etc.)")]
        public decimal OtherDeductions { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Annual Rent Paid")]
        public decimal AnnualRentPaid { get; set; }

        [Display(Name = "Residing in a Metro City")]
        public bool IsMetroCity { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Landlord PAN")]
        public string? LandlordPAN { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Mirrors Application.DTOs.Taxation.TaxDeclarationVerifyActionDto
    // exactly.
    public class TaxDeclarationVerifyActionDto
    {
        [MaxLength(1000)]
        public string? VerifierRemarks { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
