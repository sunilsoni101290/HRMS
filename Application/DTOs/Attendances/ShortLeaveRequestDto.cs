using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
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
}
