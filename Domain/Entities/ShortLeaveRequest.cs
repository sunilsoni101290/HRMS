using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee-submitted request for a FEW HOURS off during a working day
    // (e.g. "2 hours off this afternoon for a doctor's appointment") -
    // deliberately distinct from a full-day or half-day LeaveApplication
    // (LeaveApplication.IsHalfDay already covers the half-day case).
    // Approval is single-level (the requesting employee's direct
    // ReportingManagerId, or HR/Admin as a permission-based override) -
    // same pattern as WfhRequest/OnDutyRequest.
    //
    // Unlike WFH/On Duty, approving a Short Leave request does NOT write
    // back to Attendance - the employee is still physically present/working
    // most of the day. Instead, on approval, TotalHours is converted to a
    // FRACTION of a day (via AttendancePolicy.ShortLeaveHoursPerDay) and
    // deducted from the chosen LeaveTypeId's balance for the employee,
    // exactly like a fractional LeaveApplication would be - see
    // ShortLeaveRequestService.ApproveAsync.
    public class ShortLeaveRequest : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Which existing LeaveType's balance this Short Leave deducts from
        // (e.g. usually Casual Leave) - any active LeaveType is selectable,
        // this is NOT restricted to a dedicated "Short Leave" LeaveType.
        [Required]
        public string LeaveTypeId { get; set; }
        public virtual LeaveType LeaveType { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan FromTime { get; set; }

        [Required]
        public TimeSpan ToTime { get; set; }

        // (ToTime - FromTime).TotalHours, computed once at creation time.
        public decimal TotalHours { get; set; }

        // TotalHours / AttendancePolicy.ShortLeaveHoursPerDay (as of the
        // moment this request was created), rounded to 3 decimal places and
        // snapshotted here so it doesn't silently change later if the
        // policy's conversion factor is subsequently edited.
        public decimal FractionalDays { get; set; }

        [Required]
        public string Reason { get; set; }

        public ShortLeaveRequestStatus Status { get; set; } = ShortLeaveRequestStatus.Pending;

        // Approval (single level - Reporting Manager or HR/Admin override).
        // The LeaveType balance is only actually deducted here, on approval
        // - never at submission - mirroring LeaveApplicationService's
        // check-at-submit/deduct-at-approve convention.
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }

        public string? RejectedBy { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime? RejectedOn { get; set; }

        // Only the requesting employee themselves may cancel, and only
        // while Status == Pending - nothing was deducted yet at that point,
        // so cancelling never needs to credit anything back.
        public DateTime? CancelledOn { get; set; }
        public string? CancelledBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "SLR";
    }
}
