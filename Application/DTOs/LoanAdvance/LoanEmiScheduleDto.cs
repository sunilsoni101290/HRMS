namespace Application.DTOs.LoanAdvance
{
    public class LoanEmiScheduleDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeLoanId { get; set; } = string.Empty;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal EmiAmount { get; set; }
        public decimal ClosingBalance { get; set; }

        /// <summary>1=Pending, 2=Recovered, 3=Skipped, 4=Waived, 5=Cancelled.</summary>
        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? RecoveredOn { get; set; }
        public string? PayrollId { get; set; }
    }
}
