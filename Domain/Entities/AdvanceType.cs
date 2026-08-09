using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Master: a category of employee advance (Salary, Festival, Medical,
    /// etc.). Advances are interest-free by default and recovered over a
    /// small number of flat installments - no EMI amortization, unlike
    /// <see cref="LoanType"/>.
    /// </summary>
    public class AdvanceType : BaseEntity
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

        /// <summary>Flat cap. NULL means eligibility is salary-multiplier based instead.</summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>e.g. 1.0 = up to one month's gross salary. NULL means MaxAmount governs instead.</summary>
        public decimal? MaxAmountSalaryMultiplier { get; set; }

        public int MaxInstallments { get; set; } = 1;

        public bool IsInterestFree { get; set; } = true;

        // Navigation
        public virtual ICollection<EmployeeAdvance> EmployeeAdvances { get; set; }

        public override string GetSequencePrefix() => "ADT";
    }
}
