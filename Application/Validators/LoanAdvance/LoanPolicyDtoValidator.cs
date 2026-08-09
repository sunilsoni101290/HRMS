using Application.DTOs.LoanAdvance;
using FluentValidation;

namespace Application.Validators.LoanAdvance
{
    /// <summary>Validates the LoanPolicy header plus its nested ApprovalLevels matrix in one pass.</summary>
    public class LoanPolicyDtoValidator : AbstractValidator<LoanPolicyDto>
    {
        public LoanPolicyDtoValidator()
        {
            RuleFor(x => x.LoanTypeId)
                .NotEmpty().WithMessage("Loan Type is required.");

            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Min Amount cannot be negative.");

            RuleFor(x => x.MaxAmount)
                .GreaterThan(0).WithMessage("Max Amount must be greater than zero.")
                .GreaterThanOrEqualTo(x => x.MinAmount).WithMessage("Max Amount must be greater than or equal to Min Amount.");

            RuleFor(x => x.MinTenureMonths)
                .GreaterThan(0).WithMessage("Min Tenure Months must be greater than zero.");

            RuleFor(x => x.MaxTenureMonths)
                .GreaterThanOrEqualTo(x => x.MinTenureMonths).WithMessage("Max Tenure Months must be greater than or equal to Min Tenure Months.");

            RuleFor(x => x.InterestRatePercent)
                .InclusiveBetween(0, 100).When(x => x.InterestRatePercent.HasValue)
                .WithMessage("Interest Rate override must be between 0 and 100%.");

            RuleFor(x => x.MinServiceMonthsRequired)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum Service Months cannot be negative.");

            RuleFor(x => x.MaxActiveLoans)
                .GreaterThan(0).WithMessage("Max Active Loans must be at least 1.");

            RuleFor(x => x.MaxDeductionPercentOfNetSalary)
                .InclusiveBetween(1, 100).WithMessage("Max Deduction % of Net Salary must be between 1 and 100.");

            RuleFor(x => x.EligibilitySalaryMultiplier)
                .GreaterThan(0).WithMessage("Eligibility Salary Multiplier must be greater than zero.");

            RuleFor(x => x.PreClosurePenaltyPercent)
                .InclusiveBetween(0, 100).WithMessage("Pre-Closure Penalty % must be between 0 and 100.");

            RuleFor(x => x.EffectiveFrom)
                .NotEmpty().WithMessage("Effective From date is required.");

            RuleFor(x => x.EffectiveTo)
                .GreaterThan(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue)
                .WithMessage("Effective To must be after Effective From.");

            RuleFor(x => x.ApprovalLevels)
                .NotEmpty().WithMessage("At least one approval level is required.");

            RuleForEach(x => x.ApprovalLevels).SetValidator(new LoanPolicyApprovalLevelDtoValidator());

            // Level numbers must be a contiguous 1..N sequence with no gaps
            // or duplicates - EmployeeLoanService walks CurrentApprovalLevel
            // sequentially and would otherwise stall on a missing level.
            RuleFor(x => x.ApprovalLevels)
                .Must(levels =>
                {
                    var numbers = levels.Select(l => l.LevelNumber).OrderBy(n => n).ToList();
                    return numbers.SequenceEqual(Enumerable.Range(1, numbers.Count));
                })
                .When(x => x.ApprovalLevels.Count > 0)
                .WithMessage("Approval Levels must be numbered sequentially starting at 1, with no gaps or duplicates.");
        }
    }

    public class LoanPolicyApprovalLevelDtoValidator : AbstractValidator<LoanPolicyApprovalLevelDto>
    {
        public LoanPolicyApprovalLevelDtoValidator()
        {
            RuleFor(x => x.LevelNumber)
                .GreaterThan(0).WithMessage("Level Number must be greater than zero.");

            RuleFor(x => x.ApproverType)
                .InclusiveBetween(1, 3).WithMessage("Approver Type must be 1 (Reporting Manager), 2 (Specific Role) or 3 (Specific User).");

            RuleFor(x => x.ApproverRoleId)
                .NotEmpty().WithMessage("Approver Role is required when Approver Type is Specific Role.")
                .When(x => x.ApproverType == 2);

            RuleFor(x => x.ApproverUserId)
                .NotEmpty().WithMessage("Approver User is required when Approver Type is Specific User.")
                .When(x => x.ApproverType == 3);

            RuleFor(x => x.MinAmountThreshold)
                .GreaterThanOrEqualTo(0).WithMessage("Min Amount Threshold cannot be negative.");
        }
    }
}
