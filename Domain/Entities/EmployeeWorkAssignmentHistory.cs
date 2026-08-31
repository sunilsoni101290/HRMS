using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Audit trail of every status change/action on an EmployeeWorkAssignment
    /// (spec section 25 - "keep an audit/history of assignment changes").
    /// Mirrors DailyWorkLogApprovalHistory's shape exactly (ActionBy/Action/
    /// ActionDate/Remarks) for the same reason that one exists - a single
    /// status column only ever shows the CURRENT state, never how it got
    /// there.
    /// </summary>
    public class EmployeeWorkAssignmentHistory : BaseEntity
    {
        [Required]
        public string EmployeeWorkAssignmentId { get; set; } = "";

        [ForeignKey(nameof(EmployeeWorkAssignmentId))]
        public virtual EmployeeWorkAssignment? EmployeeWorkAssignment { get; set; }

        [Required]
        public string ActionBy { get; set; } = "";

        public AssignmentStatus Action { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "EWH";
    }
}
