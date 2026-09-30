using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.Services.Tracking;

namespace Waggo.Application.Tracking.Queries.GetRoute;

/// <summary>RF-008/RF-011: the owner (or the walker) sees the route so far and its summary.</summary>
internal sealed class GetRouteHandler(
    IWalkRepository walks,
    ITrackPointRepository points,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IQueryHandler<GetRouteQuery, RouteResponse>
{
    public async Task<WaggoResponse<RouteResponse>> HandleAsync(
        GetRouteQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.WalkId, cancellationToken);
        if (walk is null || (walk.OwnerId != currentUser.Id && walk.WalkerId != currentUser.Id))
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.WalkId} not found for {currentUser.Id}");
        }

        IReadOnlyList<TrackPoint> route = await points.ListByWalkAsync(walk.Id, cancellationToken);
        RoutePointResponse[] ordered =
        [
            .. route.OrderBy(point => point.RecordedAt)
                .Select(point => new RoutePointResponse(
                    point.Location.Latitude,
                    point.Location.Longitude,
                    point.RecordedAt)),
        ];

        return new WaggoResponse<RouteResponse>
        {
            Data = new RouteResponse(
                ordered,
                Math.Round(RouteMetrics.DistanceKm(route), 2),
                RouteMetrics.ElapsedMinutes(walk.StartedAt, walk.FinishedAt, timeProvider.GetUtcNow())),
        };
    }
}
