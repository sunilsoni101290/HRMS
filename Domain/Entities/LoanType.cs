using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Master: a category of employee loan (Personal, Vehicle, Emergency,
    /// etc.). Defines the DEFAULTS a <see cref="LoanPolicy"/> can override
    /// per Company/Branch - see LoanPolicy.InterestRatePercent.
    /// </summary>
    public class LoanType : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public InterestMethod InterestMethod { get; set; } = InterestMethod.Reducing;

        public decimal DefaultInterestRatePercent { get; set; }

        public int MaxTenureMonths { get; set; }

        public bool RequiresGuarantor { get; set; }

        public bool RequiresCollateral { get; set; }

        // Navigation
        public virtual ICollection<LoanPolicy> LoanPolicies { get; set; }
        public virtual ICollection<EmployeeLoan> EmployeeLoans { get; set; }

        public override string GetSequencePrefix() => "LNT";
    }
}
