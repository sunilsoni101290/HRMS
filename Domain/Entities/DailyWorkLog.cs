using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Header record - ONE row per Employee + WorkDate - carrying the
    /// Draft/Submitted/Approved/Rejected workflow status (spec section 17)
    /// and the Team Leader approval decision (spec section 18). The
    /// individual activity rows the employee enters for that date live in
    /// DailyWorkEntry (line items) below, FK'd to this header - this
    /// header/line split (rather than a Status column on every line, as
    /// literally sketched in the original spec) is what lets a Team Leader
    /// approve/reject a whole day - "8 hours across 3 jobs" - as a single
    /// decision, matching the spec's own Approval Screen mockup (section
    /// 19) which lists one row per Employee+Date.
    ///
    /// Reuses the existing ApprovalStatus enum (Draft/Pending/Approved/
    /// Rejected/ReturnedToEmployee, etc.) rather than inventing a new
    /// status enum - Pending here means "Submitted, awaiting Team Leader".
    /// </summary>
    public class DailyWorkLog : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; } = "";

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        public DateTime WorkDate { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

        /// <summary>Snapshot of Attendance.TotalWorkingHours for this Employee/Date at the time of submit - "Clocked Hours" (spec section 16). Never a duplicate source of truth; re-read from Attendance on submit, not entered manually.</summary>
        public decimal? ClockedHours { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }

        public DateTime? RejectedAt { get; set; }
        public string? RejectedBy { get; set; }
        public string? RejectionReason { get; set; }

        public virtual ICollection<DailyWorkEntry>? Entries { get; set; }

        public override string GetSequencePrefix() => "DWL";
    }
}
