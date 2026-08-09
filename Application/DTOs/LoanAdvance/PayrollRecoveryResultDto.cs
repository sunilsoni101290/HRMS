namespace Application.DTOs.LoanAdvance
{
    /// <summary>
    /// Result of IPayrollLoanRecoveryService.RecoverForPayrollAsync for a
    /// single Payroll record - one call = one employee's payroll period.
    /// Returned back up to PayrollBusinessService.GenerateAsync so any
    /// skip/shortfall messages can be surfaced on the same
    /// PayrollGenerateResultDto.Messages list the person running payroll
    /// already reads.
    /// </summary>
    public class PayrollRecoveryResultDto
    {
        public string PayrollId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;

        public int LoanInstallmentsRecovered { get; set; }
        public int AdvanceInstallmentsRecovered { get; set; }
        public int InstallmentsSkipped { get; set; }

        public decimal TotalLoanRecovered { get; set; }
        public decimal TotalAdvanceRecovered { get; set; }
        public decimal TotalRecovered => TotalLoanRecovered + TotalAdvanceRecovered;

        /// <summary>How many loans/advances reached zero outstanding and were auto-closed this run.</summary>
        public int AccountsAutoClosed { get; set; }

        /// <summary>Human-readable notes - shortfalls, already-processed guard hits, auto-closures, etc.</summary>
        public List<string> Messages { get; set; } = new();
    }
}
