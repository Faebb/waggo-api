using Waggo.Domain.Entities.Tracking;

namespace Waggo.Application.Common.Interfaces.Tracking;

/// <summary>Storage of the GPS positions of the walks (RF-008).</summary>
public interface ITrackPointRepository
{
    /// <summary>Saves a batch of positions right away.</summary>
    Task AddRangeAsync(IReadOnlyList<TrackPoint> points, CancellationToken cancellationToken);

    /// <summary>Positions of a walk in the order they were recorded.</summary>
    Task<IReadOnlyList<TrackPoint>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken);
}
