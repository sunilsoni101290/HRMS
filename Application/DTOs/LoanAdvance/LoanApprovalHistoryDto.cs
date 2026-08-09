namespace Application.DTOs.LoanAdvance
{
    public class LoanApprovalHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeLoanId { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public string CheckerId { get; set; } = string.Empty;
        public string? CheckerName { get; set; }
        public string? ActedAsDelegateForUserId { get; set; }
        public string? ActedAsDelegateForUserName { get; set; }

        /// <summary>1=Approved, 2=Rejected.</summary>
        public int Decision { get; set; }
        public string? DecisionName { get; set; }

        public string? Remarks { get; set; }
        public DateTime ActionOn { get; set; }
    }
}
