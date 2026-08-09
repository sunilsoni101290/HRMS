using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>Mirror of <see cref="LoanApprovalHistory"/> for <see cref="EmployeeAdvance"/>.</summary>
    public class AdvanceApprovalHistory : BaseEntity
    {
        [Required]
        public string EmployeeAdvanceId { get; set; }
        public virtual EmployeeAdvance EmployeeAdvance { get; set; }

        public int LevelNumber { get; set; }

        [Required]
        public string CheckerId { get; set; }
        public virtual User Checker { get; set; }

        public string? ActedAsDelegateForUserId { get; set; }
        public virtual User? ActedAsDelegateForUser { get; set; }

        public ApprovalDecision Decision { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime ActionOn { get; set; }

        public override string GetSequencePrefix() => "AAH";
    }
}
