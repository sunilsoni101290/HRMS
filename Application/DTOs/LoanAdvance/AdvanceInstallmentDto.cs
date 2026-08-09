namespace Application.DTOs.LoanAdvance
{
    public class AdvanceInstallmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeAdvanceId { get; set; } = string.Empty;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }

        /// <summary>1=Pending, 2=Recovered, 3=Skipped, 4=Waived, 5=Cancelled.</summary>
        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? RecoveredOn { get; set; }
        public string? PayrollId { get; set; }
    }
}
