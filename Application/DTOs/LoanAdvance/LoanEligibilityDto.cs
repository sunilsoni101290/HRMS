using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>
    /// Response of ILoanCalculationService.CheckEligibilityAsync (Phase 8) -
    /// shown on the request form BEFORE submit so the employee sees their
    /// real ceiling instead of discovering a rejection after the fact.
    /// </summary>
    public class LoanEligibilityDto
    {
        public bool IsEligible { get; set; }

        /// <summary>Populated only when IsEligible is false.</summary>
        public string? ReasonIfNotEligible { get; set; }

        public decimal MaxEligibleAmount { get; set; }
        public int MaxEligibleTenureMonths { get; set; }

        public decimal CurrentTotalOutstanding { get; set; }
        public int CurrentActiveLoanCount { get; set; }

        public decimal MonthlyGrossSalary { get; set; }
        public decimal MonthlyNetSalary { get; set; }
        public decimal ExistingMonthlyDeductions { get; set; }

        /// <summary>Max additional EMI the employee can take on without breaching LoanPolicy.MaxDeductionPercentOfNetSalary.</summary>
        public decimal MaxAdditionalMonthlyDeduction { get; set; }
    }

    /// <summary>Live EMI preview as the employee adjusts amount/tenure on the request form - computed, never persisted.</summary>
    public class EmiPreviewRequestDto
    {
        [Required(ErrorMessage = "Loan Type is required.")]
        public string LoanTypeId { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Range(1, 360, ErrorMessage = "Tenure Months must be between 1 and 360.")]
        public int TenureMonths { get; set; }
    }

    public class EmiPreviewResponseDto
    {
        public decimal MonthlyEmi { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPayable { get; set; }
        public List<LoanEmiScheduleDto> Schedule { get; set; } = new();
    }
}
