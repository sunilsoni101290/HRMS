using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// One flat installment of an <see cref="EmployeeAdvance"/>'s recovery
    /// plan - unlike <see cref="LoanEmiSchedule"/> there is no
    /// Principal/Interest split (advances are interest-free by default;
    /// an interest-bearing AdvanceType is out of v1 scope per Phase 1).
    /// </summary>
    public class AdvanceInstallment : BaseEntity
    {
        public string EmployeeAdvanceId { get; set; }
        public virtual EmployeeAdvance EmployeeAdvance { get; set; }

        public int InstallmentNumber { get; set; }

        public DateTime DueDate { get; set; }

        public decimal InstallmentAmount { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

        public DateTime? RecoveredOn { get; set; }

        public string? PayrollId { get; set; }
        public virtual Payroll? Payroll { get; set; }

        // Navigation
        public virtual ICollection<AdvancePaymentHistory> PaymentHistories { get; set; }

        public override string GetSequencePrefix() => "ADI";
    }
}
