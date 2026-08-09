using Application.DTOs.LoanAdvance;
using Application.Validators.LoanAdvance;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace LoanAdvance.Tests.Unit
{
    public class LoanSubmitDtoValidatorTests
    {
        private readonly LoanSubmitDtoValidator _validator = new();

        [Fact]
        public void Valid_Dto_PassesWithNoErrors()
        {
            var dto = new LoanSubmitDto { EmployeeId = "E1", LoanTypeId = "LT1", RequestedAmount = 50000m, TenureMonths = 12 };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Missing_Employee_FailsValidation()
        {
            var dto = new LoanSubmitDto { EmployeeId = "", LoanTypeId = "LT1", RequestedAmount = 50000m, TenureMonths = 12 };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.EmployeeId);
        }

        [Fact]
        public void Zero_RequestedAmount_FailsValidation()
        {
            var dto = new LoanSubmitDto { EmployeeId = "E1", LoanTypeId = "LT1", RequestedAmount = 0m, TenureMonths = 12 };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RequestedAmount);
        }

        [Fact]
        public void TenureMonths_OverThirtyYears_FailsValidation()
        {
            var dto = new LoanSubmitDto { EmployeeId = "E1", LoanTypeId = "LT1", RequestedAmount = 50000m, TenureMonths = 361 };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.TenureMonths);
        }
    }

    public class LoanApprovalActionDtoValidatorTests
    {
        private readonly LoanApprovalActionDtoValidator _validator = new();

        [Fact]
        public void Reject_WithoutRemarks_FailsValidation()
        {
            var dto = new LoanApprovalActionDto { EmployeeLoanId = "L1", Decision = 2, Remarks = null };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Remarks);
        }

        [Fact]
        public void Approve_WithoutRemarks_PassesValidation()
        {
            // Remarks are only required when rejecting (Decision == 2) - see
            // the .When(x => x.Decision == 2) condition on that rule.
            var dto = new LoanApprovalActionDto { EmployeeLoanId = "L1", Decision = 1, Remarks = null };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Remarks);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void Decision_OutOfRange_FailsValidation(int decision)
        {
            var dto = new LoanApprovalActionDto { EmployeeLoanId = "L1", Decision = decision, Remarks = "x" };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Decision);
        }
    }

    public class LoanDisbursementDtoValidatorTests
    {
        private readonly LoanDisbursementDtoValidator _validator = new();

        [Fact]
        public void FirstEmiDueDate_NotAfterDisbursedOn_FailsValidation()
        {
            var disbursedOn = new DateTime(2026, 9, 1);

            var dto = new LoanDisbursementDto
            {
                EmployeeLoanId = "L1",
                DisbursedAmount = 10000m,
                DisbursedOn = disbursedOn,
                DisbursementMode = 1,
                FirstEmiDueDate = disbursedOn // same date, not after - should fail
            };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstEmiDueDate);
        }

        [Fact]
        public void Valid_Dto_PassesWithNoErrors()
        {
            var dto = new LoanDisbursementDto
            {
                EmployeeLoanId = "L1",
                DisbursedAmount = 10000m,
                DisbursedOn = new DateTime(2026, 9, 1),
                DisbursementMode = 1,
                FirstEmiDueDate = new DateTime(2026, 10, 1)
            };

            _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }
    }

    public class LoanPolicyDtoValidatorTests
    {
        private readonly LoanPolicyDtoValidator _validator = new();

        private static LoanPolicyDto ValidBase() => new()
        {
            LoanTypeId = "LT1",
            MinAmount = 0,
            MaxAmount = 100000m,
            MinTenureMonths = 1,
            MaxTenureMonths = 12,
            MaxDeductionPercentOfNetSalary = 40m,
            EligibilitySalaryMultiplier = 10m,
            PreClosurePenaltyPercent = 2m,
            EffectiveFrom = DateTime.UtcNow,
            ApprovalLevels = new List<LoanPolicyApprovalLevelDto>
            {
                new() { LevelNumber = 1, ApproverType = 1, MinAmountThreshold = 0 }
            }
        };

        [Fact]
        public void Valid_Policy_PassesValidation()
        {
            _validator.TestValidate(ValidBase()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void MaxAmount_LessThanMinAmount_FailsValidation()
        {
            var dto = ValidBase();
            dto.MinAmount = 50000m;
            dto.MaxAmount = 10000m;

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.MaxAmount);
        }

        [Fact]
        public void NoApprovalLevels_FailsValidation()
        {
            var dto = ValidBase();
            dto.ApprovalLevels = new List<LoanPolicyApprovalLevelDto>();

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ApprovalLevels);
        }

        [Fact]
        public void ApprovalLevels_WithGapInSequence_FailsValidation()
        {
            // Levels 1 and 3 - no level 2. EmployeeLoanService walks
            // CurrentApprovalLevel sequentially and would stall on this.
            var dto = ValidBase();
            dto.ApprovalLevels = new List<LoanPolicyApprovalLevelDto>
            {
                new() { LevelNumber = 1, ApproverType = 1, MinAmountThreshold = 0 },
                new() { LevelNumber = 3, ApproverType = 1, MinAmountThreshold = 0 }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.ApprovalLevels)
                .WithErrorMessage("Approval Levels must be numbered sequentially starting at 1, with no gaps or duplicates.");
        }

        [Fact]
        public void ApprovalLevels_WithDuplicateLevelNumber_FailsValidation()
        {
            var dto = ValidBase();
            dto.ApprovalLevels = new List<LoanPolicyApprovalLevelDto>
            {
                new() { LevelNumber = 1, ApproverType = 1, MinAmountThreshold = 0 },
                new() { LevelNumber = 1, ApproverType = 1, MinAmountThreshold = 5000 }
            };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ApprovalLevels);
        }

        [Fact]
        public void SpecificRoleApproverType_WithoutApproverRoleId_FailsValidation()
        {
            var dto = ValidBase();
            dto.ApprovalLevels = new List<LoanPolicyApprovalLevelDto>
            {
                new() { LevelNumber = 1, ApproverType = 2, ApproverRoleId = null, MinAmountThreshold = 0 }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor("ApprovalLevels[0].ApproverRoleId");
        }
    }
}
