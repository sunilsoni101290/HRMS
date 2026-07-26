using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.RejoiningHistoryDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/RejoiningController.cs
    // (api/rejoining). Phase 5 (final) of the "Probation & Confirmation"
    // (Employee Lifecycle) module - NO maker-checker workflow, a single-step
    // HR-permission-gated rehire action. Append-only audit log, not
    // editable/deletable.
    public class RejoiningHistoryDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public DateTime PreviousRelievingDate { get; set; }
        public DateTime PreviousJoiningDate { get; set; }
        public DateTime NewJoiningDate { get; set; }

        public string? Reason { get; set; }

        public string ProcessedByUserId { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime ProcessedOn { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Body for POST api/rejoining - mirrors
    // Application.DTOs.EmployeeLifecycle.RejoinEmployeeDto exactly.
    // EmployeeId is the FORMER employee being rehired
    // (Employee.RelievingDate != null). NewJoiningDate must be on/after the
    // employee's current RelievingDate - validated server-side.
    public class RejoinEmployeeDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "New Joining Date")]
        public DateTime NewJoiningDate { get; set; }

        public string? Reason { get; set; }
    }

    // "Picker" row for a FORMER employee eligible to rejoin - mirrors
    // Application.DTOs.EmployeeLifecycle.RejoiningEligibleEmployeeDto
    // exactly. Returned by GET api/rejoining/eligible.
    public class RejoiningEligibleEmployeeDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }

        public DateTime JoiningDate { get; set; }
        public DateTime RelievingDate { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
    }
}
