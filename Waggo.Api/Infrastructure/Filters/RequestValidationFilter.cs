using FluentValidation;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Common;

namespace Waggo.Api.Infrastructure.Filters;

/// <summary>
/// First validation layer: validates the request DTO (<typeparamref name="TRequest"/>) with its FluentValidation
/// validator before the endpoint runs. Business rules are validated later, in the Application handler.
/// </summary>
internal sealed class RequestValidationFilter<TRequest>(
    IValidator<TRequest> validator,
    ILogger<RequestValidationFilter<TRequest>> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            throw new InvalidOperationException($"The endpoint has no argument of type {typeof(TRequest).Name}.");
        }

        WaggoResponse<TRequest> validation =
            await validator.ValidateToResponseAsync(request, context.HttpContext.RequestAborted);

        if (!validation.IsValid)
        {
            validation.WriteLogs(logger, $"{typeof(TRequest).Name} validation");
            return validation.ToApiResult();
        }

        return await next(context);
    }
}
