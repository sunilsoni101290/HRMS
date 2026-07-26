using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    // Rejoining - the FIFTH and FINAL phase of the "Probation & Confirmation"
    // (Employee Lifecycle) module (see Domain/Entities/ProbationConfirmation.cs
    // Phase 1, Domain/Entities/PipRecord.cs Phase 2,
    // Domain/Entities/EmployeeTransfer.cs Phase 3,
    // Domain/Entities/EmployeeFeedback.cs Phase 4). UNLIKE Phases 1-3, this
    // feature has NO maker-checker workflow (same as Phase 4) - it is a
    // SIMPLE, single-step, HR-permission-gated action: rehiring a FORMER
    // employee (Employee.RelievingDate != null, IsDeleted == false).
    //
    // This row IS the audit trail for the rejoin action -
    // EmployeeService.UpdateAsync itself keeps no history for these fields.
    // Purely additive/parallel to EmployeeService; does not modify it.
    //
    // PreviousRelievingDate/PreviousJoiningDate are captured automatically
    // from the Employee's CURRENT values immediately before
    // RejoiningService.RejoinAsync mutates them - never caller-supplied -
    // defense in depth against stale/manipulated "previous" data, same
    // reasoning as EmployeeTransfer's From* snapshot fields.
    //
    // An employee can rejoin more than once over their lifetime, so there
    // may be multiple RejoiningHistory rows for the same EmployeeId.
    public class RejoiningHistory : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Snapshot of Employee.RelievingDate as it was immediately before
        // this rejoin cleared it - required for audit (proves the employee
        // really had exited before being rejoined).
        [Required]
        public DateTime PreviousRelievingDate { get; set; }

        // Snapshot of Employee.JoiningDate as it was immediately before this
        // rejoin overwrote it.
        [Required]
        public DateTime PreviousJoiningDate { get; set; }

        // The new joining date being set on the live Employee record as
        // part of this rejoin.
        [Required]
        public DateTime NewJoiningDate { get; set; }

        // Optional remarks on why they're rejoining / rehire notes.
        public string? Reason { get; set; }

        // The acting User.Id (NOT EmployeeId) of whoever performed the
        // rejoin action - same "identity is about who's logged in"
        // reasoning as MakerId/GivenByUserId in the other four Phase
        // entities in this module.
        [Required]
        public string ProcessedByUserId { get; set; }

        [Required]
        public DateTime ProcessedOn { get; set; }

        public override string GetSequencePrefix() => "RJN";
    }
}
