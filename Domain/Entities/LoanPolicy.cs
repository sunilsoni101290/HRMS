using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Company/Branch (or Tenant-wide when CompanyId/BranchId are NULL)
    /// scoped rule set for a <see cref="LoanType"/>: amount/tenure bounds,
    /// eligibility formula, max deduction cap, and the approval matrix
    /// (<see cref="LoanPolicyApprovalLevel"/>). Versioned - a new edit
    /// creates a new row/VersionNumber rather than mutating an existing
    /// one, because <see cref="EmployeeLoan.LoanPolicyId"/> snapshots the
    /// exact version used at submission and must never be retroactively
    /// changed by a later policy edit. See EmployeeLoanService.SubmitAsync
    /// for how the active policy is resolved.
    /// </summary>
    public class LoanPolicy : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        /// <summary>NULL = applies to every Company in the Tenant.</summary>
        public string? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        /// <summary>NULL = applies to every Branch in the Company.</summary>
        public string? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        [Required]
        public string LoanTypeId { get; set; }
        public virtual LoanType LoanType { get; set; }

        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }

        public int MinTenureMonths { get; set; }
        public int MaxTenureMonths { get; set; }

        /// <summary>Overrides LoanType.DefaultInterestRatePercent when set.</summary>
        public decimal? InterestRatePercent { get; set; }

        /// <summary>Minimum months of continuous service before an employee is eligible.</summary>
        public int MinServiceMonthsRequired { get; set; }

        /// <summary>Max simultaneously-Active loans of this LoanType per employee.</summary>
        public int MaxActiveLoans { get; set; } = 1;

        /// <summary>Total EMI + existing deductions can never exceed this % of net salary.</summary>
        public decimal MaxDeductionPercentOfNetSalary { get; set; } = 40m;

        /// <summary>Max eligible amount = this × monthly gross salary.</summary>
        public decimal EligibilitySalaryMultiplier { get; set; } = 10m;

        /// <summary>Penalty %, applied to outstanding principal on early pre-closure.</summary>
        public decimal PreClosurePenaltyPercent { get; set; }

        public int VersionNumber { get; set; } = 1;

        public DateTime EffectiveFrom { get; set; }

        /// <summary>NULL = current/latest version.</summary>
        public DateTime? EffectiveTo { get; set; }

        // Navigation
        public virtual ICollection<LoanPolicyApprovalLevel> ApprovalLevels { get; set; }
        public virtual ICollection<EmployeeLoan> EmployeeLoans { get; set; }

        public override string GetSequencePrefix() => "LPO";
    }
}
