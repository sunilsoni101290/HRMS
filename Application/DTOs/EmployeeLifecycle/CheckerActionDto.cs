using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // Shared shape for both Approve and Reject (the Checker's action) -
    // CheckerRemarks is optional for Approve, required for Reject (see
    // ProbationConfirmationService.RejectAsync, which enforces that
    // separately since a single DTO can't express "required on one call,
    // optional on the other" via attributes alone).
    public class CheckerActionDto
    {
        [MaxLength(1000)]
        public string? CheckerRemarks { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }
}
