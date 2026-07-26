using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.PipRecordDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/PipController.cs
    // (api/pip). FinalOutcome/Status are ints on the wire - see
    // EnumExtensions.PipFinalOutcome/PipOutcomeStatus for the values. The
    // Application-side source DTO carries no data annotations at all, so
    // none are added here either - this is a read/response shape, never
    // itself posted as a form body.
    public class PipRecordDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string ProbationConfirmationId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Goals { get; set; }

        public DateTime? MidReviewDate { get; set; }
        public string? MidReviewNotes { get; set; }

        public int FinalOutcome { get; set; }
        public string? FinalOutcomeName { get; set; }

        // ---- Maker ---- (nullable: a freshly-created PIP has no Maker
        // until ProposeOutcome is called - unlike ProbationConfirmation,
        // where the Maker is set at creation time)
        public string? MakerId { get; set; }
        public string? MakerName { get; set; }
        public DateTime? MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        // ---- Checker ----
        public string? CheckerId { get; set; }
        public string? CheckerName { get; set; }
        public DateTime? CheckerActionOn { get; set; }
        public string? CheckerRemarks { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Body for POST api/pip - HR's creation input, hand-off from an
    // Approved ProbationConfirmation with Recommendation == PlaceOnPIP.
    // Mirrors Application.DTOs.EmployeeLifecycle.CreatePipRecordDto exactly,
    // annotations included.
    public class CreatePipRecordDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

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

    // Body for PUT api/pip/{id}/propose-outcome - the Maker's proposal
    // input for a PIP's final outcome resolution. Mirrors
    // Application.DTOs.EmployeeLifecycle.ProposePipOutcomeDto exactly.
    // FinalOutcome must be Successful (2) or Unsuccessful (3) -
    // EnumExtensions.PipFinalOutcome; InProgress (1) is not a valid
    // proposal value (validated server-side).
    public class ProposePipOutcomeDto
    {
        [Required]
        public int FinalOutcome { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }
    }
}
