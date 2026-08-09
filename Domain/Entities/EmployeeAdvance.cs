using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// An employee's advance request and, once disbursed, the live
    /// recovery record - near-exact mirror of <see cref="EmployeeLoan"/>
    /// minus interest amortization (per this codebase's existing
    /// convention of near-identical mirrored entities, e.g.
    /// OnDutyRequest/WfhRequest). Same configurable N-level Maker-Checker
    /// shape, resolved from the same <see cref="LoanPolicyApprovalLevel"/>
    /// matrix type (an AdvanceType has no separate policy table - the
    /// approval matrix on the employee's applicable LoanPolicy for their
    /// Company/Branch is reused; see AdvancePolicyResolver in Phase 8).
    /// </summary>
    public class EmployeeAdvance : BaseEntity
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
        public string AdvanceTypeId { get; set; }
        public virtual AdvanceType AdvanceType { get; set; }

        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }

        public int InstallmentCount { get; set; }

        [MaxLength(500)]
        public string? Purpose { get; set; }

        public AdvanceStatus Status { get; set; } = AdvanceStatus.Draft;

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
        public decimal OutstandingAmount { get; set; }

        public DateTime? ClosedOn { get; set; }

        // Navigation
        public virtual ICollection<AdvanceApprovalHistory> ApprovalHistories { get; set; }
        public virtual ICollection<AdvanceInstallment> Installments { get; set; }
        public virtual ICollection<AdvancePaymentHistory> PaymentHistories { get; set; }

        public override string GetSequencePrefix() => "ADV";
    }
}
