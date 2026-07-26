using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Performance Improvement Plan (PIP) - the SECOND phase of the
    // "Probation & Confirmation" module (see
    // Domain/Entities/ProbationConfirmation.cs, Phase 1). A PipRecord is
    // ONLY ever created as a system/HR hand-off from an Approved
    // ProbationConfirmation row whose Recommendation == PlaceOnPIP - see
    // PipService.CreateAsync. Creation itself has NO maker-checker gate
    // (it's an automatic HR action); the maker-checker workflow here
    // applies instead to the FINAL OUTCOME RESOLUTION - HR (maker)
    // proposes Successful or Unsuccessful as the outcome via
    // ProposeOutcomeAsync, and a DIFFERENT person (checker) must Approve
    // or Reject that proposal via ApproveOutcomeAsync/RejectOutcomeAsync -
    // see PipService for the actingUserId != MakerId invariant (identical
    // to ProbationConfirmationService's, no override, checked BEFORE any
    // permission check).
    public class PipRecord : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // FK back-reference to the triggering ProbationConfirmation record
        // - the Approved row with Recommendation == PlaceOnPIP that caused
        // this PIP to be created. Required; one PIP per triggering
        // confirmation (enforced in PipService.CreateAsync).
        [Required]
        public string ProbationConfirmationId { get; set; }
        public virtual ProbationConfirmation ProbationConfirmation { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // The improvement objectives/goals text set at creation.
        [Required]
        public string Goals { get; set; }

        public DateTime? MidReviewDate { get; set; }

        [MaxLength(2000)]
        public string? MidReviewNotes { get; set; }

        // The LIVE outcome - stays InProgress until a proposed outcome is
        // Approved by a checker (see ProposedFinalOutcome below).
        public PipFinalOutcome FinalOutcome { get; set; } = PipFinalOutcome.InProgress;

        // Staging field for the Maker's proposed outcome (Successful/
        // Unsuccessful) while it awaits checker action - kept distinct
        // from the live FinalOutcome, which must not change until
        // Approved. Cleared back to null on Reject. See
        // PipService.ProposeOutcomeAsync/ApproveOutcomeAsync/
        // RejectOutcomeAsync.
        public PipFinalOutcome? ProposedFinalOutcome { get; set; }

        // ---------------- Maker (proposer of the outcome) ----------------
        // The acting User.Id (NOT EmployeeId) of whoever proposed the
        // final outcome - maker-checker is about WHO is logged in, not
        // which Employee record they represent. Populated only once
        // ProposeOutcomeAsync has been called at least once.
        public string? MakerId { get; set; }

        public DateTime? MakerActionOn { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }

        public PipOutcomeStatus Status { get; set; } = PipOutcomeStatus.PendingChecker;

        // ---------------- Checker (approver/rejecter) ----------------
        // The acting User.Id of whoever approved/rejected - MUST differ
        // from MakerId. Enforced in
        // PipService.ApproveOutcomeAsync/RejectOutcomeAsync.
        public string? CheckerId { get; set; }
        public DateTime? CheckerActionOn { get; set; }

        [MaxLength(1000)]
        public string? CheckerRemarks { get; set; }

        public override string GetSequencePrefix() => "PIP";
    }
}
