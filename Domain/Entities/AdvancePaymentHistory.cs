using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>Mirror of <see cref="LoanPaymentHistory"/> for <see cref="EmployeeAdvance"/> (no Principal/Interest split).</summary>
    public class AdvancePaymentHistory : BaseEntity
    {
        public string EmployeeAdvanceId { get; set; }
        public virtual EmployeeAdvance EmployeeAdvance { get; set; }

        public string? AdvanceInstallmentId { get; set; }
        public virtual AdvanceInstallment? AdvanceInstallment { get; set; }

        public PaymentSource PaymentSource { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? PayrollId { get; set; }
        public virtual Payroll? Payroll { get; set; }

        [MaxLength(100)]
        public string? ReceiptReference { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "APH";
    }
}
