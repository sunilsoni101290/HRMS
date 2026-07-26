using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee-submitted request to be treated as "On Duty" (client visit/
    // site visit/training/external assignment, etc.) for a date RANGE
    // (FromDate..ToDate, inclusive) - near-exact mirror of WfhRequest (see
    // Domain/Entities/WfhRequest.cs), just with a Purpose instead of a
    // Reason and an optional Location, and no monthly-limit/policy check
    // (there is no MaxOnDutyDaysPerMonth on AttendancePolicy, unlike WFH's
    // MaxWfhDaysPerMonth - by deliberate design, OD has no monthly cap).
    // Approval is single-level (the requesting employee's direct
    // ReportingManagerId, or HR/Admin as a permission-based override). On
    // approval, an Attendance row is upserted for EVERY date in the range
    // with Status = AttendanceStatus.OnDuty - see
    // OnDutyRequestService.ApplyOnDutyToAttendanceAsync.
    public class OnDutyRequest : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        // Inclusive day count (ToDate.Date - FromDate.Date).Days + 1),
        // computed once at creation time.
        public int TotalDays { get; set; }

        // WFH's equivalent field is called "Reason" - named "Purpose" here
        // since On Duty is typically for a specific work purpose (client
        // visit/site visit/training) rather than a general reason.
        [Required]
        public string Purpose { get; set; }

        // Optional, purely informational (e.g. client site name/city) - no
        // validation, genuinely new field WFH doesn't have.
        public string? Location { get; set; }

        public OnDutyRequestStatus Status { get; set; } = OnDutyRequestStatus.Pending;

        // Approval (single level - Reporting Manager or HR/Admin override).
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }

        public string? RejectedBy { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime? RejectedOn { get; set; }

        // Only the requesting employee themselves may cancel, and only
        // while Status == Pending - see OnDutyRequestService.CancelAsync.
        public DateTime? CancelledOn { get; set; }
        public string? CancelledBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "OD";
    }
}
