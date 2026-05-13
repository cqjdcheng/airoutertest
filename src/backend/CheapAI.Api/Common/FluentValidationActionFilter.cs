using CheapAI.Application.Common.Responses;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CheapAI.Api.Common;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validators = context.HttpContext.RequestServices.GetServices(validatorType);
            foreach (var validator in validators.Cast<IValidator>())
            {
                var validationContextType = typeof(ValidationContext<>).MakeGenericType(argument.GetType());
                var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument)!;
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                failures.AddRange(result.Errors.Where(x => x is not null));
            }
        }

        if (failures.Count > 0)
        {
            context.Result = new BadRequestObjectResult(ApiResponseFactory.Failure(
                context.HttpContext,
                40001,
                "Validation failed",
                failures.Select(x => new
                {
                    field = x.PropertyName,
                    message = x.ErrorMessage
                })));
            return;
        }

        await next();
    }
}
