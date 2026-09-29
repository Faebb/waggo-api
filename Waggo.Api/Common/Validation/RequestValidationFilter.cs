using FluentValidation;
using Waggo.Api.Common.Responses;
using Waggo.Application.Common.Logging;
using Waggo.Application.Common.Validation;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Validation;

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

public static class RequestValidationExtensions
{
    /// <summary>Runs the DTO validator of <typeparamref name="TRequest"/> before the endpoint.</summary>
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .AddEndpointFilter<RequestValidationFilter<TRequest>>()
            .Produces<WaggoApiResponse<object>>(StatusCodes.Status400BadRequest);
    }
}
