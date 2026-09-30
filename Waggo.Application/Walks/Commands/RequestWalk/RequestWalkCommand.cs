using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Application.Walks.Commands.RequestWalk;

/// <summary><paramref name="ScheduledFor"/> null means "now", like asking for a ride.</summary>
public sealed record RequestWalkCommand(
    IReadOnlyList<Guid> PetIds,
    WalkType WalkType,
    int DurationMinutes,
    string PickupAddress,
    double Latitude,
    double Longitude,
    DateTimeOffset? ScheduledFor,
    string? Notes) : ICommand<WalkResponse>;
