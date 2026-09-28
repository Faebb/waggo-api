using Waggo.Application.Abstractions;
using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Pricing;

namespace Waggo.Api.Endpoints;

internal static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/pricing").WithTags("Pricing");

        // RF-019: fare quote shown to the owner before confirming a walk.
        group.MapGet("/quote", async (
                WalkType walkType,
                int durationMinutes,
                IQueryHandler<QuoteFareQuery, FareQuoteResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new QuoteFareQuery(walkType, durationMinutes), ct).ConfigureAwait(false);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .WithName("QuoteFare")
            .Produces<FareQuoteResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }
}
