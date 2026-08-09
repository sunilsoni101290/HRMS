using Application.DTOs.LoanAdvance;
using FluentValidation;

namespace Application.Validators.LoanAdvance
{
    /// <summary>Maker action - see IEmployeeLoanService.SubmitAsync.</summary>
    public class LoanSubmitDtoValidator : AbstractValidator<LoanSubmitDto>
    {
        public LoanSubmitDtoValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Employee is required.");
            RuleFor(x => x.LoanTypeId).NotEmpty().WithMessage("Loan Type is required.");

            RuleFor(x => x.RequestedAmount)
                .GreaterThan(0).WithMessage("Requested Amount must be greater than zero.");

            RuleFor(x => x.TenureMonths)
                .GreaterThan(0).WithMessage("Tenure Months must be greater than zero.")
                .LessThanOrEqualTo(360).WithMessage("Tenure Months looks unreasonably large (over 30 years).");

            RuleFor(x => x.Purpose)
                .MaximumLength(500);
        }
    }

    /// <summary>Checker action at the request's current approval level - see IEmployeeLoanService.ApproveAsync/RejectAsync.</summary>
    public class LoanApprovalActionDtoValidator : AbstractValidator<LoanApprovalActionDto>
    {
        public LoanApprovalActionDtoValidator()
        {
            RuleFor(x => x.EmployeeLoanId).NotEmpty().WithMessage("EmployeeLoanId is required.");

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

    /// <summary>Finance action - see IEmployeeLoanService.DisburseAsync.</summary>
    public class LoanDisbursementDtoValidator : AbstractValidator<LoanDisbursementDto>
    {
        public LoanDisbursementDtoValidator()
        {
            RuleFor(x => x.EmployeeLoanId).NotEmpty().WithMessage("EmployeeLoanId is required.");

            RuleFor(x => x.DisbursedAmount)
                .GreaterThan(0).WithMessage("Disbursed Amount must be greater than zero.");

            RuleFor(x => x.DisbursedOn)
                .NotEmpty().WithMessage("Disbursed On date is required.");

            RuleFor(x => x.DisbursementMode)
                .InclusiveBetween(1, 3).WithMessage("Disbursement Mode must be 1 (Bank Transfer), 2 (Cheque) or 3 (Payroll Credit).");

            RuleFor(x => x.DisbursementReference)
                .MaximumLength(100);

            RuleFor(x => x.FirstEmiDueDate)
                .NotEmpty().WithMessage("First EMI Due Date is required.")
                .GreaterThan(x => x.DisbursedOn).WithMessage("First EMI Due Date must be after the Disbursed On date.");
        }
    }

    /// <summary>Body confirming a pre-closure/settlement lump-sum payment - see IEmployeeLoanService.SettleAsync.</summary>
    public class LoanSettlementDtoValidator : AbstractValidator<LoanSettlementDto>
    {
        public LoanSettlementDtoValidator()
        {
            RuleFor(x => x.EmployeeLoanId).NotEmpty().WithMessage("EmployeeLoanId is required.");

            RuleFor(x => x.AmountPaid)
                .GreaterThan(0).WithMessage("Amount Paid must be greater than zero.");

            RuleFor(x => x.PaymentDate)
                .NotEmpty().WithMessage("Payment Date is required.");

            RuleFor(x => x.ReceiptReference)
                .MaximumLength(100);

            RuleFor(x => x.Remarks)
                .MaximumLength(500);

            RuleFor(x => x.ClosureReason)
                .InclusiveBetween(2, 4).WithMessage("Closure Reason must be 2 (Pre-Closed), 3 (Settled On Exit) or 4 (Written Off).");
        }
    }

    /// <summary>Live, non-persisted EMI preview - see IEmployeeLoanService.PreviewEmiAsync.</summary>
    public class EmiPreviewRequestDtoValidator : AbstractValidator<EmiPreviewRequestDto>
    {
        public EmiPreviewRequestDtoValidator()
        {
            RuleFor(x => x.LoanTypeId).NotEmpty().WithMessage("Loan Type is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.TenureMonths)
                .GreaterThan(0).WithMessage("Tenure Months must be greater than zero.")
                .LessThanOrEqualTo(360).WithMessage("Tenure Months looks unreasonably large (over 30 years).");
        }
    }
}
