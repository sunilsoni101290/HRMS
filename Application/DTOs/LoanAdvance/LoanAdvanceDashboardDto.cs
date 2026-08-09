namespace Application.DTOs.LoanAdvance
{
    /// <summary>Aggregate KPI shape for the Loan &amp; Advance dashboard (Phase 11/14) - see ILoanReportService.GetDashboardAsync.</summary>
    public class LoanAdvanceDashboardDto
    {
        public decimal TotalLoanDisbursed { get; set; }
        public decimal TotalLoanOutstanding { get; set; }
        public decimal TotalAdvanceDisbursed { get; set; }
        public decimal TotalAdvanceOutstanding { get; set; }

        public int PendingApprovalCount { get; set; }
        public int OverdueInstallmentCount { get; set; }

        public decimal ThisMonthRecoveredAmount { get; set; }
        public decimal ThisMonthDisbursedAmount { get; set; }

        /// <summary>Requests awaiting the CURRENT user's action at their assigned level - drives the "Approvals Pending on Me" widget.</summary>
        public int PendingOnMeCount { get; set; }
    }

    /// <summary>Row shape for the "Outstanding Balance" and "Employee-wise Outstanding" reports.</summary>
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

    /// <summary>Row shape for the combined Loan + Advance Payment History report.</summary>
    public class LoanAdvancePaymentReportRowDto
    {
        /// <summary>"Loan" or "Advance".</summary>
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
