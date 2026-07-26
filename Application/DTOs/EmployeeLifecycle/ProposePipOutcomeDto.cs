using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // Maker's proposal input for a PIP's final outcome resolution - see
    // PipService.ProposeOutcomeAsync. FinalOutcome must be Successful or
    // Unsuccessful (Domain.Enums.EnumExtensions.PipFinalOutcome); InProgress
    // is not a valid proposal value (validated server-side).
    public class ProposePipOutcomeDto
    {
        [Required]
        public int FinalOutcome { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }
    }
}
