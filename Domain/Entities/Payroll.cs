using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Payroll : BaseEntity
    {
        
        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        // Employee
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Month (IMPORTANT 🔥)
        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }

        public DateTime SalaryDate { get; set; }

        // Summary
        public decimal GrossSalary { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }

        // Attendance Integration
        // TotalWorkingDays = the DENOMINATOR actually used for this
        // payroll's per-day rate (calendar days in the month, or the
        // company's FixedWorkingDaysPerMonth - see ProrationBasisUsed).
        // PresentDays = deduplicated Present-equivalent attendance days
        // (see SalaryCalculationService for the dedupe rule). LeaveDays is
        // kept for backward compatibility (Unpaid Leave + Absent, i.e. the
        // NON-payable leave bucket) - PaidLeaveDays/UnpaidLeaveDays/
        // PayableDays below are the new, precise breakdown.
        public decimal? TotalWorkingDays { get; set; }
        public decimal? PresentDays { get; set; }
        public decimal? LeaveDays { get; set; }

        // Approved-leave days (from LeaveApplication, LeaveType.IsPaid) that
        // fall in this payroll's month - read directly from the Leave
        // module rather than from Attendance (see SalaryCalculationService's
        // remarks for why: Attendance is never auto-synced from approved
        // leave in this codebase).
        public decimal? PaidLeaveDays { get; set; }
        public decimal? UnpaidLeaveDays { get; set; }

        // Present + Paid Leave, capped to TotalWorkingDays and to the
        // employee's actual employed window within the month (Joining/
        // Relieving Date) - the exact numerator used in
        // NetSalary-per-component = ComponentAmount * PayableDays / TotalWorkingDays.
        public decimal? PayableDays { get; set; }

        // Which basis this specific payroll's TotalWorkingDays was computed
        // under (the company's AttendancePolicy setting AT THE TIME this
        // payroll was generated/recalculated) - kept on the row itself
        // (not just looked up live from AttendancePolicy) so a later policy
        // change never silently reinterprets an already-generated payroll's
        // numbers; a real change in policy only affects payrolls
        // generated/recalculated after that change.
        public SalaryProrationBasis? ProrationBasisUsed { get; set; }

        // Status
        public string Status { get; set; } // Draft / Processed / Paid

        // Recalculation audit (see PayrollAuditLog for the full per-event
        // trail) - these three are a cheap "has this ever been touched
        // again" summary directly on the row, so a list screen doesn't need
        // to join PayrollAuditLogs just to show a "Recalculated" badge.
        public int RecalculatedCount { get; set; } = 0;
        public DateTime? LastRecalculatedOn { get; set; }
        public string? LastRecalculatedBy { get; set; }

        // Navigation
        public ICollection<PayrollDetail> PayrollDetails { get; set; }
        public override string GetSequencePrefix() => "PRL";
    }
}
