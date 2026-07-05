using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class LeaveApprovalHistory : BaseEntity
    {
        public string LeaveApplicationId { get; set; }
        public LeaveApplication LeaveApplication { get; set; }

        public string ActionBy { get; set; }
        public ApprovalStatus Action { get; set; } // Approved / Rejected

        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; }
        public override string GetSequencePrefix() => "LAH";
    }
}
