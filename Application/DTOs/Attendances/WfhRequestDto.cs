using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
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
}
