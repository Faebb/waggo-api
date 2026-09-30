using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Payments;
using Waggo.Application.Payments.Queries.GetMyEarnings;
using Waggo.Application.Payments.Queries.GetWalkPayment;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Payments;

/// <summary>RF-015 – RF-018: the payment of a walk (owner and walker) and the walker's earnings.</summary>
internal static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder routes)
    {
        // Owner or assigned walker: the handler checks who is in the walk.
        routes.MapGet("/walks/{id:guid}/payment", GetWalkPaymentAsync)
            .WithTags("Payments")
            .WithName("GetWalkPayment")
            .RequireAuthorization()
            .Produces<WaggoApiResponse<WalkPaymentResponse>>();

        routes.MapGet("/payments/earnings", GetMyEarningsAsync)
            .WithTags("Payments")
            .WithName("GetMyEarnings")
            .RequireAuthorization(WaggoPolicies.Walker)
            .Produces<WaggoApiResponse<WalkerEarningsResponse>>();

        return routes;
    }

    private static async Task<IResult> GetWalkPaymentAsync(
        Guid id,
        IQueryHandler<GetWalkPaymentQuery, WalkPaymentResponse> handler,
        ILogger<GetWalkPaymentQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkPaymentResponse> response =
            await handler.HandleAsync(new GetWalkPaymentQuery(id), cancellationToken);
        response.WriteLogs(logger, "GetWalkPayment");
        return response.ToApiResult();
    }

    private static async Task<IResult> GetMyEarningsAsync(
        IQueryHandler<GetMyEarningsQuery, WalkerEarningsResponse> handler,
        ILogger<GetMyEarningsQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkerEarningsResponse> response =
            await handler.HandleAsync(new GetMyEarningsQuery(), cancellationToken);
        response.WriteLogs(logger, "GetMyEarnings");
        return response.ToApiResult();
    }
}
