using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.LoanAdvance.LoanTypeDto exactly - property
    // names/types must match the API's JSON 1:1 or model binding silently
    // breaks. Backed by API/Controllers/LoanTypeController.cs
    // (api/loantype). Annotations mirror the server-side
    // LoanTypeDtoValidator (Phase 9) so jQuery unobtrusive validation
    // catches the same issues before a round trip.
    public class LoanTypeDto
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        [Required(ErrorMessage = "Code is required.")]
        [StringLength(20)]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // EnumExtensions.LoanInterestMethod - 1=Reducing, 2=Flat.
        [Range(1, 2, ErrorMessage = "Interest Method must be Reducing or Flat.")]
        [Display(Name = "Interest Method")]
        public int InterestMethod { get; set; } = 1;
        public string? InterestMethodName { get; set; }

        [Range(0, 100, ErrorMessage = "Default Interest Rate must be between 0 and 100%.")]
        [Display(Name = "Default Interest Rate (% p.a.)")]
        public decimal DefaultInterestRatePercent { get; set; }

        [Range(1, 360, ErrorMessage = "Max Tenure Months must be between 1 and 360.")]
        [Display(Name = "Max Tenure (Months)")]
        public int MaxTenureMonths { get; set; }

        [Display(Name = "Requires Guarantor")]
        public bool RequiresGuarantor { get; set; }

        [Display(Name = "Requires Collateral")]
        public bool RequiresCollateral { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public int ActiveLoanCount { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
