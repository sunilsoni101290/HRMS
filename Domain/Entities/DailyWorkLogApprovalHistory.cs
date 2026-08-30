using System;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Audit trail of every submit/approve/reject/resubmit action against a
    /// DailyWorkLog - mirrors AttendanceRegularizationApprovalHistory's
    /// shape exactly (spec section 31 - reuse the existing audit
    /// convention rather than inventing a new one).
    /// </summary>
    public class DailyWorkLogApprovalHistory : BaseEntity
    {
        public string DailyWorkLogId { get; set; } = "";

        [ForeignKey(nameof(DailyWorkLogId))]
        public virtual DailyWorkLog? DailyWorkLog { get; set; }

        public string ActionBy { get; set; } = "";

        public ApprovalStatus Action { get; set; }

        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; }

        public override string GetSequencePrefix() => "DWH";
    }
}
