using Waggo.Api.Common.Responses;
using Waggo.Api.Common.Validation;
using Waggo.Api.Security;
using Waggo.Application.Abstractions;
using Waggo.Application.Common.Logging;
using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Pricing;

internal static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/pricing").WithTags("Pricing");

        // RF-019: fare quote shown to the owner before confirming a walk.
        group.MapGet("/quote", QuoteFareAsync)
            .WithName("QuoteFare")
            .WithRequestValidation<QuoteFareRequest>()
            .RequireAuthorization(WaggoPolicies.Owner)
            .Produces<WaggoApiResponse<FareQuoteResponse>>();

        return routes;
    }

    private static async Task<IResult> QuoteFareAsync(
        [AsParameters] QuoteFareRequest request,
        IQueryHandler<QuoteFareQuery, FareQuoteResponse> handler,
        ILogger<QuoteFareRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<FareQuoteResponse> response = await handler.HandleAsync(request.ToQuery(), cancellationToken);
        response.WriteLogs(logger, "QuoteFare");
        return response.ToApiResult();
    }
}
