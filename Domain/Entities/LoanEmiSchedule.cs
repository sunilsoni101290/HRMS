using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// One installment row of an <see cref="EmployeeLoan"/>'s amortization
    /// schedule, generated in full at Disbursement by
    /// ILoanCalculationService (Phase 8) and recalculated (remaining
    /// Pending rows only) on Pre-Closure. Reducing-balance vs Flat only
    /// changes how PrincipalComponent/InterestComponent split
    /// EmiAmount per row - the schedule shape is identical either way.
    /// </summary>
    public class LoanEmiSchedule : BaseEntity
    {
        public string EmployeeLoanId { get; set; }
        public virtual EmployeeLoan EmployeeLoan { get; set; }

        /// <summary>1-based installment sequence.</summary>
        public int InstallmentNumber { get; set; }

        public DateTime DueDate { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal EmiAmount { get; set; }
        public decimal ClosingBalance { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

        public DateTime? RecoveredOn { get; set; }

        /// <summary>The per-employee-per-month Payroll record this installment was recovered against (see Domain/Entities/Payroll.cs).</summary>
        public string? PayrollId { get; set; }
        public virtual Payroll? Payroll { get; set; }

        // Navigation
        public virtual ICollection<LoanPaymentHistory> PaymentHistories { get; set; }

        public override string GetSequencePrefix() => "LES";
    }
}
