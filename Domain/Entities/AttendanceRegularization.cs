using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee-submitted request to correct a day's attendance - either a
    // missing punch (forgot to punch In and/or Out) or a wrong punch time
    // that was captured. Deliberately one unified request type instead of a
    // "MissingPunch vs WrongTime" enum - the employee just states what the
    // First-In/Last-Out SHOULD have been for the date, and the approval
    // write-back only overwrites whichever of the two was actually requested.
    public class AttendanceRegularization : BaseEntity
    {
        // Employee
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        // The attendance date being regularized (calendar date, not a punch
        // timestamp).
        public DateTime Date { get; set; }

        // Nullable - the Attendance row for this Employee/Date may not exist
        // yet if the punch was fully missed (both First-In and Last-Out).
        // Populated with the resolved/created Attendance.Id once the request
        // reaches final approval.
        public string? AttendanceId { get; set; }
        public Attendance Attendance { get; set; }

        // Snapshot of what Attendance had at the time the request was raised
        // (for the Details screen / audit trail) - null if there was no
        // Attendance row at all (fully missed punch).
        public DateTime? OriginalFirstIn { get; set; }
        public DateTime? OriginalLastOut { get; set; }

        // What the employee says First-In/Last-Out should actually be.
        // Nullable independently - a request may only be correcting the
        // In, only the Out, or both.
        public DateTime? RequestedFirstIn { get; set; }
        public DateTime? RequestedLastOut { get; set; }

        public string Reason { get; set; }

        // Status
        public ApprovalStatus Status { get; set; }

        // Multi-level approval: 1 = Reporting Manager, 2 = Department Head,
        // 3 = HR. Only meaningful while Status == Pending - tracks whose
        // turn it currently is in the chain. Same chain resolution as Leave
        // (LeaveApplicationService), hand-rolled again here rather than
        // shared.
        public int CurrentLevel { get; set; } = 1;

        // Approval
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public string? RejectedReason { get; set; }

        // Set when an approver sends the request back to the employee for
        // correction instead of approving/rejecting outright.
        public string? SendBackReason { get; set; }

        public override string GetSequencePrefix() => "ATR";
    }
}
