using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.Entities.Tracking;

/// <summary>A GPS position of a walk in progress, with the moment the phone recorded it (RF-008).</summary>
public sealed class TrackPoint
{
    // Used by EF Core to materialize the entity.
    private TrackPoint()
    {
    }

    public Guid Id { get; private set; }

    public Guid WalkId { get; private set; }

    public GeoPoint Location { get; private set; } = null!;

    public DateTimeOffset RecordedAt { get; private set; }

    public static TrackPoint Record(Guid walkId, GeoPoint location, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(location);
        return new TrackPoint
        {
            Id = Guid.CreateVersion7(recordedAt),
            WalkId = walkId,
            Location = location,
            RecordedAt = recordedAt,
        };
    }
}
