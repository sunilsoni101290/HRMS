using Application.DTOs.LoanAdvance;
using FluentValidation;

namespace Application.Validators.LoanAdvance
{
    /// <summary>
    /// Server-side authoritative validation for LoanTypeDto (Create/Update)
    /// - registered globally via API/Filters/FluentValidationActionFilter.cs,
    /// see Program.cs's AddValidatorsFromAssemblyContaining call. Mirrors
    /// the DataAnnotations already on LoanTypeDto (Phase 9's client-side
    /// half) but is the one actually enforced server-side.
    /// </summary>
    public class LoanTypeDtoValidator : AbstractValidator<LoanTypeDto>
    {
        public LoanTypeDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(20);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.InterestMethod)
                .InclusiveBetween(1, 2).WithMessage("Interest Method must be 1 (Reducing) or 2 (Flat).");

            RuleFor(x => x.DefaultInterestRatePercent)
                .InclusiveBetween(0, 100).WithMessage("Default Interest Rate must be between 0 and 100%.");

            RuleFor(x => x.MaxTenureMonths)
                .GreaterThan(0).WithMessage("Max Tenure Months must be greater than zero.")
                .LessThanOrEqualTo(360).WithMessage("Max Tenure Months looks unreasonably large (over 30 years).");
        }
    }
}
