using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.LoanAdvance.LoanPolicyDto exactly - see
    // LoanTypeDto's remarks. Backed by
    // API/Controllers/LoanPolicyController.cs (api/loanpolicy). Saving an
    // edit creates a NEW row (VersionNumber + 1) rather than mutating the
    // existing one - see LoanPolicyController's remarks - so Id is only
    // ever used to load a starting point for the edit form.
    public class LoanPolicyDto
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        [Display(Name = "Company (blank = all)")]
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        [Display(Name = "Branch (blank = all)")]
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        [Required(ErrorMessage = "Loan Type is required.")]
        [Display(Name = "Loan Type")]
        public string LoanTypeId { get; set; } = string.Empty;
        public string? LoanTypeName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min Amount cannot be negative.")]
        [Display(Name = "Min Amount")]
        public decimal MinAmount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Max Amount must be greater than zero.")]
        [Display(Name = "Max Amount")]
        public decimal MaxAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Min Tenure Months must be greater than zero.")]
        [Display(Name = "Min Tenure (Months)")]
        public int MinTenureMonths { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max Tenure Months must be greater than zero.")]
        [Display(Name = "Max Tenure (Months)")]
        public int MaxTenureMonths { get; set; }

        [Range(0, 100, ErrorMessage = "Interest Rate override must be between 0 and 100%.")]
        [Display(Name = "Interest Rate Override (% p.a.)")]
        public decimal? InterestRatePercent { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum Service Months cannot be negative.")]
        [Display(Name = "Min Service (Months)")]
        public int MinServiceMonthsRequired { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max Active Loans must be at least 1.")]
        [Display(Name = "Max Active Loans")]
        public int MaxActiveLoans { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Max Deduction % of Net Salary must be between 1 and 100.")]
        [Display(Name = "Max Deduction (% of Net Salary)")]
        public decimal MaxDeductionPercentOfNetSalary { get; set; } = 40m;

        [Range(0.01, double.MaxValue, ErrorMessage = "Eligibility Salary Multiplier must be greater than zero.")]
        [Display(Name = "Eligibility Salary Multiplier")]
        public decimal EligibilitySalaryMultiplier { get; set; } = 10m;

        [Range(0, 100, ErrorMessage = "Pre-Closure Penalty % must be between 0 and 100.")]
        [Display(Name = "Pre-Closure Penalty (%)")]
        public decimal PreClosurePenaltyPercent { get; set; }

        public int VersionNumber { get; set; } = 1;

        [Required(ErrorMessage = "Effective From date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Effective From")]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        public DateTime? EffectiveTo { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<LoanPolicyApprovalLevelDto> ApprovalLevels { get; set; } = new();

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    public class LoanPolicyApprovalLevelDto
    {
        public string? Id { get; set; }
        public string? LoanPolicyId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Level Number must be greater than zero.")]
        public int LevelNumber { get; set; }

        // EnumExtensions.LoanApproverType - 1=ReportingManager, 2=SpecificRole, 3=SpecificUser.
        [Range(1, 3, ErrorMessage = "Approver Type must be Reporting Manager, Specific Role or Specific User.")]
        public int ApproverType { get; set; } = 1;
        public string? ApproverTypeName { get; set; }

        public string? ApproverRoleId { get; set; }
        public string? ApproverRoleName { get; set; }

        public string? ApproverUserId { get; set; }
        public string? ApproverUserName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min Amount Threshold cannot be negative.")]
        public decimal MinAmountThreshold { get; set; }
    }
}
