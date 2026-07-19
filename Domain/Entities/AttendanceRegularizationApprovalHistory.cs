using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class AttendanceRegularizationApprovalHistory : BaseEntity
    {
        public string AttendanceRegularizationId { get; set; }
        public AttendanceRegularization AttendanceRegularization { get; set; }

        public string ActionBy { get; set; }
        public ApprovalStatus Action { get; set; } // Approved / Rejected / etc.

        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; }
        public override string GetSequencePrefix() => "ARH";
    }
}
