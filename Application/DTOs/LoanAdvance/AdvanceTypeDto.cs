using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>Read/write shape for Domain.Entities.AdvanceType. See LoanTypeDto's remarks for the client/server validation split.</summary>
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

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal? MaxAmount { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal? MaxAmountSalaryMultiplier { get; set; }

        [Range(1, 60, ErrorMessage = "Max Installments must be between 1 and 60.")]
        public int MaxInstallments { get; set; } = 1;
        public bool IsInterestFree { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int ActiveAdvanceCount { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
