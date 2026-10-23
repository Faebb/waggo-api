using Waggo.Api.Endpoints.Payments.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Payments;
using Waggo.Application.Payments.Commands.RegisterPaymentMethod;
using Waggo.Application.Payments.Queries.GetMyEarnings;
using Waggo.Application.Payments.Queries.GetWalkPayment;
using Waggo.Application.Payments.Queries.ListMyPaymentMethods;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Payments;

/// <summary>
/// RF-015 – RF-018: the payment of a walk (owner and walker) and the walker's earnings.
/// RF-020: the owner's simulated payment methods.
/// </summary>
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

        routes.MapPost("/payments/methods", RegisterPaymentMethodAsync)
            .WithTags("Payments")
            .WithName("RegisterPaymentMethod")
            .RequireAuthorization(WaggoPolicies.Owner)
            .WithRequestValidation<RegisterPaymentMethodRequest>()
            .Produces<WaggoApiResponse<PaymentMethodResponse>>();

        routes.MapGet("/payments/methods", ListMyPaymentMethodsAsync)
            .WithTags("Payments")
            .WithName("ListMyPaymentMethods")
            .RequireAuthorization(WaggoPolicies.Owner)
            .Produces<WaggoApiResponse<IReadOnlyList<PaymentMethodResponse>>>();

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

    private static async Task<IResult> RegisterPaymentMethodAsync(
        RegisterPaymentMethodRequest request,
        ICommandHandler<RegisterPaymentMethodCommand, PaymentMethodResponse> handler,
        ILogger<RegisterPaymentMethodRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<PaymentMethodResponse> response =
            await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "RegisterPaymentMethod");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListMyPaymentMethodsAsync(
        IQueryHandler<ListMyPaymentMethodsQuery, IReadOnlyList<PaymentMethodResponse>> handler,
        ILogger<ListMyPaymentMethodsQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<PaymentMethodResponse>> response =
            await handler.HandleAsync(new ListMyPaymentMethodsQuery(), cancellationToken);
        response.WriteLogs(logger, "ListMyPaymentMethods");
        return response.ToApiResult();
    }
}
