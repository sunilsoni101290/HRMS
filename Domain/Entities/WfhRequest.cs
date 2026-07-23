using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee-submitted request to work from home for a date RANGE
    // (FromDate..ToDate, inclusive) - unlike Attendance Regularization
    // (single-date correction), this deliberately covers a span of days in
    // one request. Approval is single-level (the requesting employee's
    // direct ReportingManagerId, or HR/Admin as a permission-based
    // override) - simpler than Leave/Attendance Regularization's
    // multi-level chain, by deliberate design decision. On approval, an
    // Attendance row is upserted for EVERY date in the range with
    // Status = AttendanceStatus.WorkFromHome - see
    // WfhRequestService.ApplyWfhToAttendanceAsync.
    public class WfhRequest : BaseEntity
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

        [Required]
        public string Reason { get; set; }

        public WfhRequestStatus Status { get; set; } = WfhRequestStatus.Pending;

        // Approval (single level - Reporting Manager or HR/Admin override).
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }

        public string? RejectedBy { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime? RejectedOn { get; set; }

        // Only the requesting employee themselves may cancel, and only
        // while Status == Pending - see WfhRequestService.CancelAsync.
        public DateTime? CancelledOn { get; set; }
        public string? CancelledBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "WFH";
    }
}
