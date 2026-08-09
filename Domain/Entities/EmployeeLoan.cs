using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// An employee's loan request AND, once approved/disbursed, the live
    /// loan account itself - the same row progresses through the full
    /// lifecycle (see LoanStatus), exactly the same "one entity spans
    /// request through resolution" convention already used by
    /// <see cref="ProbationConfirmation"/>. Maker-Checker fields
    /// (MakerId/MakerActionOn/MakerRemarks + CurrentApprovalLevel, with the
    /// per-level Approve/Reject audit trail in
    /// <see cref="LoanApprovalHistory"/>) follow that same established
    /// shape, extended for a configurable N-level matrix instead of a
    /// single fixed checker - see
    /// Domain/Entities/LoanPolicyApprovalLevel.cs and
    /// EmployeeLoanService.ApproveAsync/RejectAsync for the
    /// actingUserId != MakerId invariant enforced at every level.
    /// </summary>
    public class EmployeeLoan : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        public string? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        public string? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public string LoanTypeId { get; set; }
        public virtual LoanType LoanType { get; set; }

        /// <summary>Snapshot of the exact LoanPolicy version applied at submission.</summary>
        [Required]
        public string LoanPolicyId { get; set; }
        public virtual LoanPolicy LoanPolicy { get; set; }

        public decimal RequestedAmount { get; set; }

        /// <summary>Set by the final approver; may differ from RequestedAmount (partial approval).</summary>
        public decimal? ApprovedAmount { get; set; }

        public int TenureMonths { get; set; }

        /// <summary>Snapshotted from LoanPolicy/LoanType at approval time - never recalculated retroactively.</summary>
        public decimal InterestRatePercent { get; set; }

        public InterestMethod InterestMethod { get; set; }

        [MaxLength(500)]
        public string? Purpose { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Draft;

        /// <summary>0 until Submitted; then the next LoanPolicyApprovalLevel.LevelNumber awaiting action.</summary>
        public int CurrentApprovalLevel { get; set; }

        // ---------------- Maker ----------------
        [Required]
        public string MakerId { get; set; }
        public virtual User Maker { get; set; }

        public DateTime MakerActionOn { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }

        // ---------------- Disbursement ----------------
        public decimal? DisbursedAmount { get; set; }
        public DateTime? DisbursedOn { get; set; }
        public DisbursementMode? DisbursementMode { get; set; }

        [MaxLength(100)]
        public string? DisbursementReference { get; set; }

        // ---------------- Running balance ----------------
        public decimal OutstandingPrincipal { get; set; }

        public DateTime? ClosedOn { get; set; }
        public LoanClosureReason? ClosureReason { get; set; }

        // Navigation
        public virtual ICollection<LoanApprovalHistory> ApprovalHistories { get; set; }
        public virtual ICollection<LoanEmiSchedule> EmiSchedules { get; set; }
        public virtual ICollection<LoanPaymentHistory> PaymentHistories { get; set; }

        public override string GetSequencePrefix() => "LNR";
    }
}
