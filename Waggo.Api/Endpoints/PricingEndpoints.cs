using Waggo.Api.Common.Responses;
using Waggo.Application.Abstractions;
using Waggo.Application.Common.Logging;
using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Api.Endpoints;

internal static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/pricing").WithTags("Pricing");

        // RF-019: fare quote shown to the owner before confirming a walk.
        group.MapGet("/quote", async (
                WalkType walkType,
                int durationMinutes,
                IQueryHandler<QuoteFareQuery, FareQuoteResponse> handler,
                ILogger<QuoteFareQuery> logger,
                CancellationToken ct) =>
            {
                QuoteFareQuery query = new(walkType, durationMinutes);
                WaggoResponse<FareQuoteResponse> response = await handler.HandleAsync(query, ct);
                return response.WriteLogs(logger, "QuoteFare").ToApiResult();
            })
            .WithName("QuoteFare")
            .Produces<WaggoApiResponse<FareQuoteResponse>>()
            .Produces<WaggoApiResponse<FareQuoteResponse>>(StatusCodes.Status400BadRequest);

        return routes;
    }
}
