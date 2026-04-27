using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Attendance : BaseEntity
    {
        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Organization
        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public string BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        // Employee
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Date
        [Required]
        public DateTime Date { get; set; }

        // Shift Mapping
        public string ShiftId { get; set; }
        public virtual Shift Shift { get; set; }

        // Punch Info
        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }

        public decimal? WorkingHours { get; set; }
        public decimal? OvertimeHours { get; set; }

        // Status
        public AttendanceStatus Status { get; set; } // Present / Absent / Leave / Holiday / WeekOff

        public bool IsLate { get; set; }
        public bool IsEarlyExit { get; set; }

        // Remarks
        public string Remarks { get; set; }

        // Audit
        public bool IsManualEntry { get; set; } = false;
        public override string GetSequencePrefix() => "ATD";
    }
}
