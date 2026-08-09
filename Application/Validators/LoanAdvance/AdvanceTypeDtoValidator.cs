using Application.DTOs.LoanAdvance;
using FluentValidation;

namespace Application.Validators.LoanAdvance
{
    public class AdvanceTypeDtoValidator : AbstractValidator<AdvanceTypeDto>
    {
        public AdvanceTypeDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(20);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.MaxAmount)
                .GreaterThan(0).When(x => x.MaxAmount.HasValue)
                .WithMessage("Max Amount must be greater than zero when specified.");

            RuleFor(x => x.MaxAmountSalaryMultiplier)
                .GreaterThan(0).When(x => x.MaxAmountSalaryMultiplier.HasValue)
                .WithMessage("Max Amount Salary Multiplier must be greater than zero when specified.");

            RuleFor(x => x.MaxInstallments)
                .GreaterThan(0).WithMessage("Max Installments must be greater than zero.")
                .LessThanOrEqualTo(60).WithMessage("Max Installments looks unreasonably large (over 5 years).");
        }
    }
}
