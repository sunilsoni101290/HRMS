using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Immutable ledger row for every rupee recovered against an
    /// <see cref="EmployeeLoan"/>, regardless of source (payroll
    /// deduction, manual receipt, or pre-closure lump sum) - this is what
    /// "Payment History" (FR-15) reads from. <see cref="LoanEmiScheduleId"/>
    /// is NULL for a pre-closure lump sum (it doesn't correspond to a
    /// single amortization row).
    /// </summary>
    public class LoanPaymentHistory : BaseEntity
    {
        public string EmployeeLoanId { get; set; }
        public virtual EmployeeLoan EmployeeLoan { get; set; }

        public string? LoanEmiScheduleId { get; set; }
        public virtual LoanEmiSchedule? LoanEmiSchedule { get; set; }

        public PaymentSource PaymentSource { get; set; }

        public decimal AmountPaid { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal InterestPaid { get; set; }

        public DateTime PaymentDate { get; set; }

        /// <summary>Set when PaymentSource == PayrollDeduction.</summary>
        public string? PayrollId { get; set; }
        public virtual Payroll? Payroll { get; set; }

        [MaxLength(100)]
        public string? ReceiptReference { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "LPH";
    }
}
