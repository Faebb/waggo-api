using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Services.Walks;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.Walks.Queries.ListAvailableWalks;

/// <summary>
/// RF-007/RF-006: the walker sees the open requests of other users, like a driver sees ride requests. With a position
/// the database returns only those within <see cref="WalkMatching.MaxDistanceKm"/>, nearest first; without it, all of
/// them by time.
/// </summary>
internal sealed class ListAvailableWalksHandler(
    IValidator<ListAvailableWalksQuery> validator,
    IWalkRepository walks,
    IWalkerProfileRepository walkerProfiles,
    ICurrentUser currentUser)
    : IQueryHandler<ListAvailableWalksQuery, IReadOnlyList<AvailableWalkResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<AvailableWalkResponse>>> HandleAsync(
        ListAvailableWalksQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        WaggoResponse<IReadOnlyList<AvailableWalkResponse>> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(query, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        await walkerProfiles.EnsureVerifiedAsync(currentUser.Id, cancellationToken);

        GeoPoint? here = query is { Latitude: { } latitude, Longitude: { } longitude }
            ? GeoPoint.Create(latitude, longitude).Data
            : null;

        if (here is null)
        {
            IReadOnlyList<Walk> all = await walks.ListRequestedAsync(cancellationToken);
            response.Data =
            [
                .. all.Where(walk => walk.OwnerId != currentUser.Id)
                    .OrderBy(walk => walk.ScheduledFor)
                    .Select(walk => AvailableWalkResponse.From(walk, distanceKm: null)),
            ];
            return response;
        }

        // Already filtered and sorted by the database; the distance is shown to the walker.
        IReadOnlyList<Walk> nearby =
            await walks.ListRequestedNearAsync(here, WalkMatching.MaxDistanceKm, cancellationToken);
        response.Data =
        [
            .. nearby.Where(walk => walk.OwnerId != currentUser.Id)
                .Select(walk => AvailableWalkResponse.From(walk, here.DistanceKmTo(walk.PickupLocation))),
        ];
        return response;
    }
}
