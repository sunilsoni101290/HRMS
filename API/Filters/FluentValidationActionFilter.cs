using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    /// <summary>
    /// Phase 9 (Validation) - global action filter that runs EVERY registered
    /// FluentValidation validator against its matching action argument
    /// before the action executes. Purely additive: it only fires for a
    /// bound argument whose type has an IValidator&lt;T&gt; registered in DI
    /// (see Program.cs's AddValidatorsFromAssemblyContaining call), so
    /// every controller in the app - not just Loan & Advance - is
    /// unaffected unless/until it registers its own validators. Errors are
    /// merged into ModelState so they come back in the same
    /// ValidationProblemDetails shape ASP.NET Core's built-in
    /// [ApiController] model-binding errors already use, giving API
    /// consumers one consistent error contract either way.
    /// </summary>
    public class FluentValidationActionFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationActionFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

                if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                    continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                        context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
                return;
            }

            await next();
        }
    }
}
