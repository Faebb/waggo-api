using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Walks;

/// <summary>A walk as the owner (or its walker) sees it. Shared by every Walks use case.</summary>
public sealed record WalkResponse(
    Guid Id,
    string Status,
    IReadOnlyList<Guid> PetIds,
    string WalkType,
    int DurationMinutes,
    string PickupAddress,
    double Latitude,
    double Longitude,
    DateTimeOffset ScheduledFor,
    string? Notes,
    string Currency,
    decimal Total,
    decimal Commission,
    decimal WalkerPayout,
    string? WalkerId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    AssignedWalkerResponse? Walker)
{
    /// <summary>
    /// Builds the response. <paramref name="walkerProfile"/> is the profile of the assigned walker, if any;
    /// <see cref="Walker"/> is null only when nobody accepted the walk.
    /// </summary>
    public static WalkResponse From(Walk walk, WalkerProfile? walkerProfile)
    {
        ArgumentNullException.ThrowIfNull(walk);
        AssignedWalkerResponse? walker = walk.WalkerId is null
            ? null
            : new AssignedWalkerResponse(walk.WalkerId, walkerProfile?.FullName);

        return new WalkResponse(
            walk.Id,
            walk.Status.ToString(),
            walk.PetIds,
            walk.WalkType.ToString(),
            walk.DurationMinutes,
            walk.PickupAddress,
            walk.PickupLocation.Latitude,
            walk.PickupLocation.Longitude,
            walk.ScheduledFor,
            walk.Notes,
            walk.Currency,
            walk.Total,
            walk.Commission,
            walk.WalkerPayout,
            walk.WalkerId,
            walk.RequestedAt,
            walk.StartedAt,
            walk.FinishedAt,
            walker);
    }
}
