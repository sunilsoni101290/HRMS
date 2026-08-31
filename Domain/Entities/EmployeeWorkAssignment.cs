using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// "Manager assigns Job/Structure/Activity to Employee" - the missing
    /// layer between the Job/Master data and DailyWorkEntry (spec sections
    /// 2/5/28/29). An employee's Daily Work Entry job/structure/activity
    /// dropdowns are scoped to their OWN active assignments (see
    /// WorkAssignmentService.GetAssignedComboAsync) rather than the full
    /// Job master, so "who is working on what" is always explicit and
    /// manager-controlled rather than free-picked by the employee.
    ///
    /// AssignedBy is deliberately a UserId (not EmployeeId) - mirrors every
    /// other audit "*By" field in this codebase (CreatedBy/ModifiedBy/
    /// ApprovedBy on DailyWorkLog etc.) which are all UserIds resolved back
    /// to a display name via Users.Employee, not a second employee FK.
    ///
    /// JobItemId/WorkActivityId are nullable because AssignmentType can be
    /// as coarse as "Job" alone (spec section 4's "Assignment Type: Job /
    /// Task / Activity") - a Job-level assignment lets the employee pick
    /// any of that Job's structures/activities in Daily Work Entry, while an
    /// Activity-level assignment pins them to one exact combination.
    /// </summary>
    public class EmployeeWorkAssignment : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; } = "";

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        /// <summary>UserId of whoever created this assignment - see class remarks. Never trusted from the client; always the acting user's own Id.</summary>
        [Required]
        public string AssignedBy { get; set; } = "";

        [Required]
        public string WorkJobId { get; set; } = "";

        [ForeignKey(nameof(WorkJobId))]
        public virtual WorkJob? WorkJob { get; set; }

        [Required]
        public string JobTypeId { get; set; } = "";

        [ForeignKey(nameof(JobTypeId))]
        public virtual JobType? JobType { get; set; }

        /// <summary>Null when AssignmentType = Job (whole job, any structure).</summary>
        public string? JobItemId { get; set; }

        [ForeignKey(nameof(JobItemId))]
        public virtual JobItem? JobItem { get; set; }

        /// <summary>Null unless AssignmentType = Activity (pinned to one exact activity).</summary>
        public string? WorkActivityId { get; set; }

        [ForeignKey(nameof(WorkActivityId))]
        public virtual WorkActivity? WorkActivity { get; set; }

        public AssignmentType AssignmentType { get; set; } = AssignmentType.Job;

        public AssignmentPriority Priority { get; set; } = AssignmentPriority.Normal;

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;

        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }

        [Range(0, 100000)]
        public decimal? EstimatedHours { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime? RejectedAt { get; set; }
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Set when this assignment was superseded by a reassignment to a
        /// different employee (spec section 25 - "if reassigned, retain the
        /// history"). The OLD assignment row is never overwritten/deleted;
        /// it is marked Returned/inactive and a brand new
        /// EmployeeWorkAssignment row is created for the new employee,
        /// linked back via ReassignedFromId.
        /// </summary>
        public string? ReassignedFromId { get; set; }

        [ForeignKey(nameof(ReassignedFromId))]
        public virtual EmployeeWorkAssignment? ReassignedFrom { get; set; }

        public virtual System.Collections.Generic.ICollection<DailyWorkEntry>? DailyWorkEntries { get; set; }

        public override string GetSequencePrefix() => "EWA";
    }
}
