using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Domain.Entities.Tracking;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Tracking;

internal sealed class TrackPointRepository(WaggoDbContext db) : ITrackPointRepository
{
    public async Task AddRangeAsync(IReadOnlyList<TrackPoint> points, CancellationToken cancellationToken)
    {
        db.TrackPoints.AddRange(points);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrackPoint>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken) =>
        await db.TrackPoints.AsNoTracking()
            .Where(point => point.WalkId == walkId)
            .OrderBy(point => point.RecordedAt)
            .ToListAsync(cancellationToken);
}
