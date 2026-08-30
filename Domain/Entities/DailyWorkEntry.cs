using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Line item - one activity worked, for one Job/JobItem/Activity, on
    /// the Employee+Date represented by the parent DailyWorkLog header.
    /// WorkJobId/JobItemId/WorkActivityId are all nullable because an Idle
    /// or Downtime line has no job at all (Excel's Example Sheet row 9:
    /// "Idle Hours" with no Job Number/Job Type/Activity, just Hours + a
    /// reason). Every FK here is re-validated server-side (JobItem belongs
    /// to WorkJob, WorkActivity belongs to the entry's JobType) - never
    /// trusted from the client (spec section 35).
    /// </summary>
    public class DailyWorkEntry : BaseEntity
    {
        [Required]
        public string DailyWorkLogId { get; set; } = "";

        [ForeignKey(nameof(DailyWorkLogId))]
        public virtual DailyWorkLog? DailyWorkLog { get; set; }

        /// <summary>Null for Idle/Downtime-only lines.</summary>
        public string? WorkJobId { get; set; }

        [ForeignKey(nameof(WorkJobId))]
        public virtual WorkJob? WorkJob { get; set; }

        /// <summary>Denormalized copy of the selected Job Type at entry time - needed even for Idle/Downtime lines (which have no WorkJob) so the activity's JobType/Category can still be validated and reported on.</summary>
        public string? JobTypeId { get; set; }

        [ForeignKey(nameof(JobTypeId))]
        public virtual JobType? JobType { get; set; }

        public string? JobItemId { get; set; }

        [ForeignKey(nameof(JobItemId))]
        public virtual JobItem? JobItem { get; set; }

        public string? WorkActivityId { get; set; }

        [ForeignKey(nameof(WorkActivityId))]
        public virtual WorkActivity? WorkActivity { get; set; }

        /// <summary>Only set when the selected activity's WorkCategory is Idle or Downtime (spec sections 11/12) - never mixed, enforced server-side by WorkEntryReason.Category matching the activity's WorkCategory.</summary>
        public string? WorkEntryReasonId { get; set; }

        [ForeignKey(nameof(WorkEntryReasonId))]
        public virtual WorkEntryReason? WorkEntryReason { get; set; }

        [Range(0.01, 24)]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "DWE";
    }
}
