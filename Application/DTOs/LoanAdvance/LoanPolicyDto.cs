using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>
    /// Read/write shape for Domain.Entities.LoanPolicy, including its
    /// nested approval matrix. Saving a policy edit creates a NEW row
    /// (VersionNumber + 1) rather than mutating the existing one - see
    /// ILoanPolicyService.UpdateAsync (Phase 6) - so this DTO's Id is only
    /// ever used to load a starting point for the edit form, never sent
    /// back as "the row to update in place". See LoanTypeDto's remarks for
    /// the client/server validation split - LoanPolicyDtoValidator also
    /// covers cross-field rules (Max &gt;= Min, sequential level numbers)
    /// that plain DataAnnotations can't express as cleanly.
    /// </summary>
    public class LoanPolicyDto
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        [Required(ErrorMessage = "Loan Type is required.")]
        public string LoanTypeId { get; set; } = string.Empty;
        public string? LoanTypeName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min Amount cannot be negative.")]
        public decimal MinAmount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Max Amount must be greater than zero.")]
        public decimal MaxAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Min Tenure Months must be greater than zero.")]
        public int MinTenureMonths { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max Tenure Months must be greater than zero.")]
        public int MaxTenureMonths { get; set; }

        [Range(0, 100, ErrorMessage = "Interest Rate override must be between 0 and 100%.")]
        public decimal? InterestRatePercent { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum Service Months cannot be negative.")]
        public int MinServiceMonthsRequired { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max Active Loans must be at least 1.")]
        public int MaxActiveLoans { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Max Deduction % of Net Salary must be between 1 and 100.")]
        public decimal MaxDeductionPercentOfNetSalary { get; set; } = 40m;

        [Range(0.01, double.MaxValue, ErrorMessage = "Eligibility Salary Multiplier must be greater than zero.")]
        public decimal EligibilitySalaryMultiplier { get; set; } = 10m;

        [Range(0, 100, ErrorMessage = "Pre-Closure Penalty % must be between 0 and 100.")]
        public decimal PreClosurePenaltyPercent { get; set; }

        public int VersionNumber { get; set; } = 1;

        [Required(ErrorMessage = "Effective From date is required.")]
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public List<LoanPolicyApprovalLevelDto> ApprovalLevels { get; set; } = new();

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    /// <summary>Read/write shape for one row of Domain.Entities.LoanPolicyApprovalLevel.</summary>
    public class LoanPolicyApprovalLevelDto
    {
        public string? Id { get; set; }
        public string? LoanPolicyId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Level Number must be greater than zero.")]
        public int LevelNumber { get; set; }

        /// <summary>1=ReportingManager, 2=SpecificRole, 3=SpecificUser - see Domain.Enums.EnumExtensions.ApproverType.</summary>
        [Range(1, 3, ErrorMessage = "Approver Type must be 1 (Reporting Manager), 2 (Specific Role) or 3 (Specific User).")]
        public int ApproverType { get; set; }
        public string? ApproverTypeName { get; set; }

        public string? ApproverRoleId { get; set; }
        public string? ApproverRoleName { get; set; }

        public string? ApproverUserId { get; set; }
        public string? ApproverUserName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min Amount Threshold cannot be negative.")]
        public decimal MinAmountThreshold { get; set; }
    }
}
