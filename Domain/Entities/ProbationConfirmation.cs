using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Foundational Maker-Checker (segregation-of-duties) entity for the
    // Probation & Confirmation module. A MAKER (HR staff holding Create
    // permission on AppFeatureConstants.PROBATION_CONFIRMATION) proposes an
    // outcome (Confirm/Extend/PlaceOnPIP/Terminate) for an employee whose
    // probation is due for review; a DIFFERENT person acting as CHECKER
    // (holding Approve permission) must Approve or Reject it before it
    // takes effect - see ProbationConfirmationService.ApproveAsync/
    // RejectAsync for the core invariant (actingUserId != MakerId, no
    // override, checked BEFORE any permission check).
    //
    // This exact field shape (MakerId/MakerActionOn/MakerRemarks/Status/
    // CheckerId/CheckerActionOn/CheckerRemarks) is the pattern later PIP
    // Outcome and Employee Transfer features should replicate, each with
    // its own identically-shaped status enum (PipOutcomeStatus/
    // TransferStatus) per this codebase's per-module enum convention.
    public class ProbationConfirmation : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Snapshotted from Employee.JoiningDate at Create time.
        [Required]
        public DateTime ProbationStartDate { get; set; }

        // Snapshotted at Create time from Employee.ProbationEndDate, or -
        // if that was never set - computed as
        // Employee.JoiningDate.AddMonths(Designation.ProbationPeriodMonths).
        [Required]
        public DateTime OriginalProbationEndDate { get; set; }

        public ProbationRecommendation Recommendation { get; set; }

        // Required (and only meaningful) when Recommendation == Extend -
        // the new probation end date being proposed. Applied to
        // Employee.ProbationEndDate on Approve.
        public DateTime? ExtendedProbationEndDate { get; set; }

        // ---------------- Maker (proposer) ----------------
        // The acting User.Id (NOT EmployeeId) of whoever proposed this
        // action - maker-checker is about WHO is logged in, not which
        // Employee record they represent.
        [Required]
        public string MakerId { get; set; }

        [Required]
        public DateTime MakerActionOn { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }

        public ProbationConfirmationStatus Status { get; set; } = ProbationConfirmationStatus.PendingChecker;

        // ---------------- Checker (approver/rejecter) ----------------
        // The acting User.Id of whoever approved/rejected - MUST differ
        // from MakerId. Enforced in
        // ProbationConfirmationService.ApproveAsync/RejectAsync.
        public string? CheckerId { get; set; }
        public DateTime? CheckerActionOn { get; set; }

        [MaxLength(1000)]
        public string? CheckerRemarks { get; set; }

        // Set only when Status == Approved AND Recommendation == Confirm -
        // mirrors the value written to Employee.ConfirmationDate at that
        // moment.
        public DateTime? FinalConfirmationDate { get; set; }

        public override string GetSequencePrefix() => "PBC";
    }
}
