using Application.DTOs.LoanAdvance;
using Application.Validators.LoanAdvance;
using FluentValidation.TestHelper;
using Xunit;

namespace LoanAdvance.Tests.Unit
{
    public class AdvanceSubmitDtoValidatorTests
    {
        private readonly AdvanceSubmitDtoValidator _validator = new();

        [Fact]
        public void Valid_Dto_PassesWithNoErrors()
        {
            var dto = new AdvanceSubmitDto { EmployeeId = "E1", AdvanceTypeId = "AT1", RequestedAmount = 5000m, InstallmentCount = 3 };

            _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InstallmentCount_OverFiveYears_FailsValidation()
        {
            var dto = new AdvanceSubmitDto { EmployeeId = "E1", AdvanceTypeId = "AT1", RequestedAmount = 5000m, InstallmentCount = 61 };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.InstallmentCount);
        }

        [Fact]
        public void Missing_AdvanceType_FailsValidation()
        {
            var dto = new AdvanceSubmitDto { EmployeeId = "E1", AdvanceTypeId = "", RequestedAmount = 5000m, InstallmentCount = 3 };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.AdvanceTypeId);
        }
    }

    public class AdvanceApprovalActionDtoValidatorTests
    {
        private readonly AdvanceApprovalActionDtoValidator _validator = new();

        [Fact]
        public void Reject_WithoutRemarks_FailsValidation()
        {
            var dto = new AdvanceApprovalActionDto { EmployeeAdvanceId = "A1", Decision = 2, Remarks = null };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Remarks);
        }

        [Fact]
        public void Approve_WithoutRemarks_PassesValidation()
        {
            var dto = new AdvanceApprovalActionDto { EmployeeAdvanceId = "A1", Decision = 1, Remarks = null };

            _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Remarks);
        }
    }

    public class AdvanceDisbursementDtoValidatorTests
    {
        private readonly AdvanceDisbursementDtoValidator _validator = new();

        [Fact]
        public void FirstInstallmentDueDate_NotAfterDisbursedOn_FailsValidation()
        {
            var disbursedOn = new DateTime(2026, 9, 1);

            var dto = new AdvanceDisbursementDto
            {
                EmployeeAdvanceId = "A1",
                DisbursedAmount = 5000m,
                DisbursedOn = disbursedOn,
                DisbursementMode = 1,
                FirstInstallmentDueDate = disbursedOn.AddDays(-1)
            };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstInstallmentDueDate);
        }
    }

    public class AdvanceSettlementDtoValidatorTests
    {
        private readonly AdvanceSettlementDtoValidator _validator = new();

        [Fact]
        public void Zero_AmountPaid_FailsValidation()
        {
            var dto = new AdvanceSettlementDto { EmployeeAdvanceId = "A1", AmountPaid = 0m, PaymentDate = DateTime.UtcNow };

            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.AmountPaid);
        }

        [Fact]
        public void Valid_Dto_PassesWithNoErrors()
        {
            var dto = new AdvanceSettlementDto { EmployeeAdvanceId = "A1", AmountPaid = 5000m, PaymentDate = DateTime.UtcNow };

            _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }
    }
}
