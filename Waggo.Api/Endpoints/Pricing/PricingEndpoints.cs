using Waggo.Api.Endpoints.Pricing.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Pricing.Queries.QuoteFare;
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
