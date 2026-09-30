using Waggo.Api.Endpoints.Tracking.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Tracking;
using Waggo.Application.Tracking.Commands.RecordTrack;
using Waggo.Application.Tracking.Queries.GetRoute;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Tracking;

/// <summary>
/// RF-008/RF-011: the walker sends the route of the walk in progress; the owner and the walker read it.
/// </summary>
internal static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/walks/{id:guid}/track", RecordTrackAsync)
            .WithTags("Tracking")
            .WithName("RecordTrack")
            .RequireAuthorization(WaggoPolicies.Walker)
            .Produces<WaggoApiResponse<int>>();

        // Owner or assigned walker: the handler checks who the caller is.
        routes.MapGet("/walks/{id:guid}/track", GetRouteAsync)
            .WithTags("Tracking")
            .WithName("GetRoute")
            .RequireAuthorization()
            .Produces<WaggoApiResponse<RouteResponse>>();

        return routes;
    }

    private static async Task<IResult> RecordTrackAsync(
        Guid id,
        RecordTrackRequest request,
        ICommandHandler<RecordTrackCommand, int> handler,
        ILogger<RecordTrackRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<int> response = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        response.WriteLogs(logger, "RecordTrack");
        return response.ToApiResult();
    }

    private static async Task<IResult> GetRouteAsync(
        Guid id,
        IQueryHandler<GetRouteQuery, RouteResponse> handler,
        ILogger<GetRouteQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<RouteResponse> response = await handler.HandleAsync(new GetRouteQuery(id), cancellationToken);
        response.WriteLogs(logger, "GetRoute");
        return response.ToApiResult();
    }
}
