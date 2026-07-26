using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Attendances.CompOffCandidateDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/CompOffController.cs
    // (api/compoff). Status is an int on the wire - see
    // EnumExtensions.CompOffCandidateStatus for the values
    // (PendingReview=1, Approved=2, Rejected=3).
    //
    // Comp Off candidates are only ever created by a background job
    // (API/BackgroundServices/CompOffDetectionService.cs) - there is
    // deliberately no Create/POST endpoint, so there is no corresponding
    // "CreateCompOffCandidateDto" here.
    public class CompOffCandidateDto
    {
        public string? Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }

        public string AttendanceId { get; set; }

        public DateTime WorkedDate { get; set; }
        public decimal HoursWorked { get; set; }

        public string? TriggerReason { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedOn { get; set; }

        public string? RejectionReason { get; set; }

        public decimal CreditedDays { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    // Body for PUT api/compoff/{id}/reject.
    public class CompOffRejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
