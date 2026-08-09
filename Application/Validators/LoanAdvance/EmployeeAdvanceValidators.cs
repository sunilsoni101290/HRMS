using Application.DTOs.LoanAdvance;
using FluentValidation;

namespace Application.Validators.LoanAdvance
{
    public class AdvanceSubmitDtoValidator : AbstractValidator<AdvanceSubmitDto>
    {
        public AdvanceSubmitDtoValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Employee is required.");
            RuleFor(x => x.AdvanceTypeId).NotEmpty().WithMessage("Advance Type is required.");

            RuleFor(x => x.RequestedAmount)
                .GreaterThan(0).WithMessage("Requested Amount must be greater than zero.");

            RuleFor(x => x.InstallmentCount)
                .GreaterThan(0).WithMessage("Installment Count must be greater than zero.")
                .LessThanOrEqualTo(60).WithMessage("Installment Count looks unreasonably large (over 5 years).");

            RuleFor(x => x.Purpose)
                .MaximumLength(500);
        }
    }

    public class AdvanceApprovalActionDtoValidator : AbstractValidator<AdvanceApprovalActionDto>
    {
        public AdvanceApprovalActionDtoValidator()
        {
            RuleFor(x => x.EmployeeAdvanceId).NotEmpty().WithMessage("EmployeeAdvanceId is required.");

            RuleFor(x => x.Decision)
                .InclusiveBetween(1, 2).WithMessage("Decision must be 1 (Approved) or 2 (Rejected).");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required when rejecting.")
                .When(x => x.Decision == 2);

            RuleFor(x => x.Remarks)
                .MaximumLength(1000);

            RuleFor(x => x.ApprovedAmount)
                .GreaterThan(0).When(x => x.ApprovedAmount.HasValue)
                .WithMessage("Approved Amount must be greater than zero when specified.");
        }
    }

    public class AdvanceDisbursementDtoValidator : AbstractValidator<AdvanceDisbursementDto>
    {
        public AdvanceDisbursementDtoValidator()
        {
            RuleFor(x => x.EmployeeAdvanceId).NotEmpty().WithMessage("EmployeeAdvanceId is required.");

            RuleFor(x => x.DisbursedAmount)
                .GreaterThan(0).WithMessage("Disbursed Amount must be greater than zero.");

            RuleFor(x => x.DisbursedOn)
                .NotEmpty().WithMessage("Disbursed On date is required.");

            RuleFor(x => x.DisbursementMode)
                .InclusiveBetween(1, 3).WithMessage("Disbursement Mode must be 1 (Bank Transfer), 2 (Cheque) or 3 (Payroll Credit).");

            RuleFor(x => x.DisbursementReference)
                .MaximumLength(100);

            RuleFor(x => x.FirstInstallmentDueDate)
                .NotEmpty().WithMessage("First Installment Due Date is required.")
                .GreaterThan(x => x.DisbursedOn).WithMessage("First Installment Due Date must be after the Disbursed On date.");
        }
    }

    public class AdvanceSettlementDtoValidator : AbstractValidator<AdvanceSettlementDto>
    {
        public AdvanceSettlementDtoValidator()
        {
            RuleFor(x => x.EmployeeAdvanceId).NotEmpty().WithMessage("EmployeeAdvanceId is required.");

            RuleFor(x => x.AmountPaid)
                .GreaterThan(0).WithMessage("Amount Paid must be greater than zero.");

            RuleFor(x => x.PaymentDate)
                .NotEmpty().WithMessage("Payment Date is required.");

            RuleFor(x => x.ReceiptReference)
                .MaximumLength(100);

            RuleFor(x => x.Remarks)
                .MaximumLength(500);
        }
    }
}
