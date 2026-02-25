using FluentValidation;
using NotificationsAPI.Api.Helpers;

namespace NotificationsAPI.Api.Filters;

public class ValidationEndpointFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request == null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        if (validator == null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errorsDictionary = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ApiResponses.BadRequestMultiple("Erro de validação", errorsDictionary);
        }

        return await next(context);
    }
}
