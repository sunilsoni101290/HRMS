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

        /// <summary>
        /// Free-text "what was actually completed" - deliberately separate
        /// from WorkActivity.Name (spec section 11: "Work Activity" is the
        /// fixed master activity being charged to, "Work Done Today" is the
        /// free description of today's actual progress against it). Null
        /// for Idle/Downtime lines, where there is no "work done" to
        /// describe.
        /// </summary>
        [MaxLength(1000)]
        public string? WorkDoneToday { get; set; }

        /// <summary>
        /// Links this line back to the EmployeeWorkAssignment it is being
        /// charged against (spec sections 13/28/29: Job Master -> Job
        /// Assignment -> Employee -> Daily Work Entry). Null for
        /// Idle/Downtime lines (which have no assignment at all) and for
        /// AdhocReason-justified unassigned work (spec section 14). Every
        /// non-null value here is re-validated server-side on save - it
        /// must belong to this line's employee, be Active/Accepted/
        /// InProgress, and its Job/JobItem/WorkActivity must match this
        /// line's own values (never trusted from the client).
        /// </summary>
        public string? AssignmentId { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public virtual EmployeeWorkAssignment? Assignment { get; set; }

        /// <summary>
        /// Required justification when a Direct/Indirect line is saved
        /// WITHOUT a matching EmployeeWorkAssignment (spec section 14 -
        /// "Unassigned/Ad-hoc Work" controlled exception path). Null for
        /// every normally-assigned line and for Idle/Downtime lines (which
        /// never require an assignment in the first place).
        /// </summary>
        [MaxLength(500)]
        public string? AdhocReason { get; set; }

        public override string GetSequencePrefix() => "DWE";
    }
}
