using Waggo.Api.Infrastructure.Filters;
using Waggo.Api.Infrastructure.Responses;

namespace Waggo.Api.Infrastructure.Extensions;

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
