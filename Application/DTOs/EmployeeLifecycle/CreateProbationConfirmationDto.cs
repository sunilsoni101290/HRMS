using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // Maker's proposal input - EmployeeId is the subject employee (never
    // the maker themselves; the maker is resolved server-side from
    // actingUserId, see ProbationConfirmationService.CreateAsync).
    public class CreateProbationConfirmationDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        // Domain.Enums.EnumExtensions.ProbationRecommendation.
        [Required]
        public int Recommendation { get; set; }

        // Required only when Recommendation == Extend (2) - validated in
        // ProbationConfirmationService.CreateAsync.
        [DataType(DataType.Date)]
        public DateTime? ExtendedProbationEndDate { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }
    }
}
