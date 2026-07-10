using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class LeaveApplication : BaseEntity
    {
        // Org Mapping
        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        // Employee
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        // Leave Type
        public string LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }

        // Dates
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        // Half Day Support
        public bool IsHalfDay { get; set; }
        public HalfDayType HalfDayType { get; set; } // FirstHalf / SecondHalf

        // Reason
        public string? Reason { get; set; }

        // Status
        public ApprovalStatus Status { get; set; } //Enum = Pending / Approved / Rejected / Cancelled / ReturnedToEmployee

        // Multi-level approval: 1 = Reporting Manager, 2 = Department Head,
        // 3 = HR. Only meaningful while Status == Pending - tracks whose
        // turn it currently is in the chain.
        public int CurrentLevel { get; set; } = 1;

        // Approval
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public string? RejectedReason { get; set; }

        // Set when an approver sends the request back to the employee for
        // correction instead of approving/rejecting outright.
        public string? SendBackReason { get; set; }

        // Attachment
        public string? DocumentUrl { get; set; }
        public override string GetSequencePrefix() => "LA";
    }
}
