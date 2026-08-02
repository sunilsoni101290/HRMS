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
        // FIX (defect C1): TenantId/ActingUserId intentionally removed from
        // this DTO - they must NEVER be client-suppliable. The API controller
        // resolves both from the caller's JWT claims (see
        // API/Controllers/ProbationConfirmationController.cs) and passes them
        // into the service explicitly; trusting client-posted values here
        // previously allowed cross-tenant impersonation.

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
