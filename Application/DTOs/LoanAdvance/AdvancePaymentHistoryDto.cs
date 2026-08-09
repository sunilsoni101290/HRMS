namespace Application.DTOs.LoanAdvance
{
    public class AdvancePaymentHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeAdvanceId { get; set; } = string.Empty;
        public string? AdvanceInstallmentId { get; set; }

        /// <summary>1=PayrollDeduction, 2=ManualReceipt, 3=PreClosure.</summary>
        public int PaymentSource { get; set; }
        public string? PaymentSourceName { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PayrollId { get; set; }
        public string? ReceiptReference { get; set; }
        public string? Remarks { get; set; }
    }
}
