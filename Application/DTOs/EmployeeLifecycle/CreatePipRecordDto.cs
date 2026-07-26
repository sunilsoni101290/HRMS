using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // HR's creation input for a new PIP - no maker-checker gate on this
    // action itself (see PipService.CreateAsync); outcome fields are not
    // part of creation, they come later via ProposePipOutcomeDto.
    public class CreatePipRecordDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        // The Approved ProbationConfirmation row (Recommendation ==
        // PlaceOnPIP) that triggered this PIP - validated server-side in
        // PipService.CreateAsync.
        [Required]
        [Display(Name = "Probation Confirmation")]
        public string ProbationConfirmationId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public string Goals { get; set; }
    }
}
