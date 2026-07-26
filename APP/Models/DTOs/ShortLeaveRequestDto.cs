using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Attendances.ShortLeaveRequestDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/ShortLeaveRequestController.cs
    // (api/shortleaverequest). Status is an int on the wire - see
    // EnumExtensions.ShortLeaveRequestStatus for the values (Pending=1,
    // Approved=2, Rejected=3, Cancelled=4). Unlike WFH/OD, approving a Short
    // Leave request deducts a fractional day from the chosen LeaveType's
    // balance instead of touching Attendance.
    public class ShortLeaveRequestDto
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
        [Display(Name = "Leave Type")]
        public string LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "From Time")]
        public TimeSpan FromTime { get; set; }

        [Display(Name = "To Time")]
        public TimeSpan ToTime { get; set; }

        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Fractional Days")]
        public decimal FractionalDays { get; set; }

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

    // Body for POST api/shortleaverequest.
    public class CreateShortLeaveRequestDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Leave Type is required")]
        [Display(Name = "Leave Type")]
        public string LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "From Time is required")]
        [Display(Name = "From Time")]
        public TimeSpan FromTime { get; set; }

        [Required(ErrorMessage = "To Time is required")]
        [Display(Name = "To Time")]
        public TimeSpan ToTime { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }

    // Body for PUT api/shortleaverequest/{id}/reject.
    public class ShortLeaveRejectRequestDto
    {
        [Required(ErrorMessage = "Reason is required")]
        public string Reason { get; set; }
    }
}
