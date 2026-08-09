using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// One immutable row per approval level acted on for an
    /// <see cref="EmployeeLoan"/> - mirrors LeaveApprovalHistory /
    /// AttendanceRegularizationApprovalHistory's "append-only timeline"
    /// shape. <see cref="CheckerId"/> must never equal
    /// EmployeeLoan.MakerId - enforced in
    /// EmployeeLoanService.ApproveAsync/RejectAsync, not here (an entity
    /// has no service-layer context to validate against).
    /// </summary>
    public class LoanApprovalHistory : BaseEntity
    {
        [Required]
        public string EmployeeLoanId { get; set; }
        public virtual EmployeeLoan EmployeeLoan { get; set; }

        public int LevelNumber { get; set; }

        [Required]
        public string CheckerId { get; set; }
        public virtual User Checker { get; set; }

        /// <summary>Set when the Checker acted via an ApprovalDelegation on behalf of the originally assigned approver.</summary>
        public string? ActedAsDelegateForUserId { get; set; }
        public virtual User? ActedAsDelegateForUser { get; set; }

        public ApprovalDecision Decision { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime ActionOn { get; set; }

        public override string GetSequencePrefix() => "LAH";
    }
}
