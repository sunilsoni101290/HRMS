namespace APP.Models.DTOs
{
    // Phase 14 - APP-side mirrors of Application.DTOs.LoanAdvance's report
    // row shapes (see api/loanadvancereport/outstanding-balance and
    // api/loanadvancereport/payment-history), following this codebase's
    // established DTO-mirroring convention.

    public class OutstandingBalanceReportRowDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }

        public int ActiveLoanCount { get; set; }
        public decimal TotalLoanOutstanding { get; set; }

        public int ActiveAdvanceCount { get; set; }
        public decimal TotalAdvanceOutstanding { get; set; }

        public DateTime? NextDueDate { get; set; }
        public decimal? NextDueAmount { get; set; }
        public int MaxDaysPastDue { get; set; }
    }

    public class LoanAdvancePaymentReportRowDto
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public string? TypeName { get; set; }

        public int PaymentSource { get; set; }
        public string? PaymentSourceName { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PayrollId { get; set; }
        public string? ReceiptReference { get; set; }
    }
}
