using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.LoanAdvance.AdvanceTypeDto exactly - see
    // LoanTypeDto's remarks. Backed by
    // API/Controllers/AdvanceTypeController.cs (api/advancetype).
    public class AdvanceTypeDto
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        [Required(ErrorMessage = "Code is required.")]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Max Amount")]
        public decimal? MaxAmount { get; set; }

        [Display(Name = "Max Amount (Salary Multiplier)")]
        public decimal? MaxAmountSalaryMultiplier { get; set; }

        [Range(1, 60, ErrorMessage = "Max Installments must be between 1 and 60.")]
        [Display(Name = "Max Installments")]
        public int MaxInstallments { get; set; } = 1;

        [Display(Name = "Interest Free")]
        public bool IsInterestFree { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public int ActiveAdvanceCount { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
