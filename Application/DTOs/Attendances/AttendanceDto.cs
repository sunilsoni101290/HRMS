using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    public class AttendanceDto
    {
        public string? Id { get; set; }

        [Required]
        public string TenantId { get; set; }

        public string? TenantName { get; set; }

        [Required]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        public string? CompanyName { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        public string? BranchName { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        [Required]
        [Display(Name = "Attendance Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Shift")]
        public string? ShiftId { get; set; }

        public string? ShiftName { get; set; }

        // Attendance Timing
        [Display(Name = "First In")]
        [DataType(DataType.DateTime)]
        public DateTime? FirstIn { get; set; }

        [Display(Name = "Last Out")]
        [DataType(DataType.DateTime)]
        public DateTime? LastOut { get; set; }

        // Hours
        [Display(Name = "Working Hours")]
        public decimal TotalWorkingHours { get; set; }

        [Display(Name = "Break Hours")]
        public decimal BreakHours { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        // Status
        [Display(Name = "Attendance Status")]
        public AttendanceStatus Status { get; set; }

        [Display(Name = "Late Coming")]
        public bool IsLate { get; set; }

        [Display(Name = "Early Exit")]
        public bool IsEarlyExit { get; set; }

        [Display(Name = "Manual Entry")]
        public bool IsManualEntry { get; set; } = false;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        // Extra UI Helper Fields
        public string? StatusText
        {
            get
            {
                return Status.ToString();
            }
        }

        public string? WorkingHoursText
        {
            get
            {
                return $"{TotalWorkingHours:0.##} Hrs";
            }
        }
    }
}
