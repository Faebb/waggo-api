using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Walks;

/// <summary>
/// An open request as a walker sees it before accepting: what, when, where and how much they earn.
/// Notes and the owner's data stay hidden until the walker accepts.
/// </summary>
public sealed record AvailableWalkResponse(
    Guid Id,
    string WalkType,
    int DurationMinutes,
    int PetCount,
    string PickupAddress,
    double Latitude,
    double Longitude,
    DateTimeOffset ScheduledFor,
    string Currency,
    decimal WalkerPayout,
    double? DistanceKm)
{
    public static AvailableWalkResponse From(Walk walk, double? distanceKm)
    {
        ArgumentNullException.ThrowIfNull(walk);
        return new AvailableWalkResponse(
            walk.Id,
            walk.WalkType.ToString(),
            walk.DurationMinutes,
            walk.PetIds.Count,
            walk.PickupAddress,
            walk.PickupLocation.Latitude,
            walk.PickupLocation.Longitude,
            walk.ScheduledFor,
            walk.Currency,
            walk.WalkerPayout,
            distanceKm is null ? null : Math.Round(distanceKm.Value, 1));
    }
}
