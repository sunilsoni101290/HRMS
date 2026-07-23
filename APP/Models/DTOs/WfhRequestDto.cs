using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Attendances.WfhRequestDto exactly - property
    // names/types must match the API's JSON 1:1 or model binding silently
    // breaks. Backed by API/Controllers/WfhRequestController.cs
    // (api/wfhrequest). Status is an int on the wire - see
    // EnumExtensions.WfhRequestStatus for the values (Pending=1,
    // Approved=2, Rejected=3, Cancelled=4).
    public class WfhRequestDto
    {
        public string? Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? ReportingManagerName { get; set; }

        [Required]
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required]
        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Display(Name = "Total Days")]
        public int TotalDays { get; set; }

        [Required]
        public string Reason { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedOn { get; set; }

        public string? RejectedReason { get; set; }
        public DateTime? RejectedOn { get; set; }

        public DateTime? CancelledOn { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public string? TenantId { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Body for POST api/wfhrequest.
    public class CreateWfhRequestDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "From Date is required")]
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required")]
        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }

    // Body for PUT api/wfhrequest/{id}/reject.
    public class WfhRejectRequestDto
    {
        [Required(ErrorMessage = "Reason is required")]
        public string Reason { get; set; }
    }

    // Result of GET api/wfhrequest/remaining?employeeId=&month=&year= - used
    // by the Create form to show "X of Y WFH days used this month".
    public class WfhRemainingDaysDto
    {
        public string EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // Null/0 = no active policy or no limit configured (unlimited).
        public int? MaxWfhDaysPerMonth { get; set; }

        // Already-Approved WFH days for this employee that fall within this
        // calendar month.
        public int UsedDays { get; set; }

        // Null when MaxWfhDaysPerMonth is null/0 (unlimited); otherwise
        // Max - Used, floored at 0.
        public int? RemainingDays { get; set; }
    }
}
