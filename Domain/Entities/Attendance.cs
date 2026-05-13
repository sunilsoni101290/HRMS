using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Attendance : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public string BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string ShiftId { get; set; }
        public virtual Shift Shift { get; set; }

        // Calculated Fields
        public DateTime? FirstIn { get; set; }
        public DateTime? LastOut { get; set; }

        public decimal TotalWorkingHours { get; set; }
        public decimal BreakHours { get; set; }
        public decimal OvertimeHours { get; set; }

        public AttendanceStatus Status { get; set; }

        public bool IsLate { get; set; }
        public bool IsEarlyExit { get; set; }

        public string? Remarks { get; set; }

        public bool IsManualEntry { get; set; } = false;

        // Navigation
        public ICollection<AttendanceLog> Logs { get; set; }

        public override string GetSequencePrefix() => "ATD";
    }
}
