using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Attendances.OnDutyRequestDto exactly - property
    // names/types must match the API's JSON 1:1 or model binding silently
    // breaks. Backed by API/Controllers/OnDutyRequestController.cs
    // (api/ondutyrequest). Status is an int on the wire - see
    // EnumExtensions.OnDutyRequestStatus for the values (Pending=1,
    // Approved=2, Rejected=3, Cancelled=4). Unlike WFH, OD has no monthly
    // cap, so there is no "remaining days" endpoint/DTO here.
    public class OnDutyRequestDto
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
        public string Purpose { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

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

    // Body for POST api/ondutyrequest.
    public class CreateOnDutyRequestDto
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

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters")]
        public string Purpose { get; set; }

        [Display(Name = "Location")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }
    }

    // Body for PUT api/ondutyrequest/{id}/reject.
    public class OnDutyRejectRequestDto
    {
        [Required(ErrorMessage = "Reason is required")]
        public string Reason { get; set; }
    }
}
