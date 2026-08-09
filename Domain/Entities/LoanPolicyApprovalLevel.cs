using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// One row of a <see cref="LoanPolicy"/>'s configurable N-level approval
    /// matrix. A request only routes through levels whose
    /// <see cref="MinAmountThreshold"/> is &lt;= the request's amount, in
    /// ascending <see cref="LevelNumber"/> order - e.g. LevelNumber=1/
    /// Threshold=0 (always applies) then LevelNumber=2/Threshold=50000
    /// (only for requests &gt;= 50,000) gives a 1-level flow for small
    /// amounts and a 2-level flow for larger ones from the same policy. See
    /// EmployeeLoanService.ResolveApprovalMatrix.
    /// </summary>
    public class LoanPolicyApprovalLevel : BaseEntity
    {
        [Required]
        public string LoanPolicyId { get; set; }
        public virtual LoanPolicy LoanPolicy { get; set; }

        /// <summary>1-based sequence in which levels are evaluated.</summary>
        public int LevelNumber { get; set; }

        public ApproverType ApproverType { get; set; }

        /// <summary>Set only when ApproverType == SpecificRole.</summary>
        public string? ApproverRoleId { get; set; }
        public virtual Role? ApproverRole { get; set; }

        /// <summary>Set only when ApproverType == SpecificUser.</summary>
        public string? ApproverUserId { get; set; }
        public virtual User? ApproverUser { get; set; }

        public decimal MinAmountThreshold { get; set; }

        public override string GetSequencePrefix() => "LPL";
    }
}
