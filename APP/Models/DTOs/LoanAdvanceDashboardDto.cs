namespace APP.Models.DTOs
{
    // Phase 14 - mirrors Application.DTOs.LoanAdvance.LoanAdvanceDashboardDto
    // exactly (see api/loanadvancereport/dashboard). RecentLoans/RecentAdvances
    // are NOT part of the API dashboard payload - the controller fetches those
    // separately from the existing employeeloan/employeeadvance list endpoints
    // (cheap, already-paginated-on-client lists) and merges them in for the
    // "Recent Activity" panels.
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

        public int PendingOnMeCount { get; set; }

        public List<EmployeeLoanListDto> RecentLoans { get; set; } = new();
        public List<EmployeeAdvanceListDto> RecentAdvances { get; set; } = new();
    }
}
