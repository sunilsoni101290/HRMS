using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>
    /// Read/write shape for Domain.Entities.LoanType. Used for both
    /// Create and Edit (Id is null on Create) - same single-DTO
    /// convention already used by BiometricDeviceDto.
    ///
    /// DataAnnotations below are the CLIENT-side half of Phase 9 - jQuery
    /// unobtrusive validation will read these automatically once a Phase
    /// 11 Razor view binds asp-for to this shape. FluentValidation's
    /// LoanTypeDtoValidator (Application/Validators/LoanAdvance/) is the
    /// authoritative SERVER-side half and is what's actually enforced by
    /// the API regardless of what the browser does.
    /// </summary>
    public class LoanTypeDto
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

        /// <summary>1=Reducing, 2=Flat - see Domain.Enums.EnumExtensions.InterestMethod.</summary>
        [Range(1, 2, ErrorMessage = "Interest Method must be 1 (Reducing) or 2 (Flat).")]
        public int InterestMethod { get; set; } = 1;
        public string? InterestMethodName { get; set; }

        [Range(0, 100, ErrorMessage = "Default Interest Rate must be between 0 and 100%.")]
        public decimal DefaultInterestRatePercent { get; set; }

        [Range(1, 360, ErrorMessage = "Max Tenure Months must be between 1 and 360.")]
        public int MaxTenureMonths { get; set; }

        public bool RequiresGuarantor { get; set; }
        public bool RequiresCollateral { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Populated on read only - used by the UI to block delete/deactivate when in use.</summary>
        public int ActiveLoanCount { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
