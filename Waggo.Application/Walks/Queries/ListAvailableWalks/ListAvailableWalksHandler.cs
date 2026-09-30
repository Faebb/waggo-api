using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.Walks.Queries.ListAvailableWalks;

/// <summary>
/// RF-007: the walker sees the open requests of other users, like a driver sees ride requests. With a position they
/// are sorted by straight-line distance; without it, by time. Matching by real distance and verification is RF-006.
/// </summary>
internal sealed class ListAvailableWalksHandler(
    IValidator<ListAvailableWalksQuery> validator,
    IWalkRepository walks,
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

        GeoPoint? here = query is { Latitude: { } latitude, Longitude: { } longitude }
            ? GeoPoint.Create(latitude, longitude).Data
            : null;

        IReadOnlyList<Walk> requested = await walks.ListRequestedAsync(cancellationToken);
        IEnumerable<AvailableWalkResponse> offers = requested
            .Where(walk => walk.OwnerId != currentUser.Id)
            .Select(walk => AvailableWalkResponse.From(walk, here?.DistanceKmTo(walk.PickupLocation)));

        response.Data = here is null
            ? [.. offers.OrderBy(offer => offer.ScheduledFor)]
            : [.. offers.OrderBy(offer => offer.DistanceKm)];
        return response;
    }
}
