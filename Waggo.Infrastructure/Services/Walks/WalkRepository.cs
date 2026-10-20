using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.ValueObjects.Walks;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Walks;

internal sealed class WalkRepository(WaggoDbContext db) : IWalkRepository
{
    public async Task AddAsync(Walk walk, CancellationToken cancellationToken)
    {
        db.Walks.Add(walk);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Walk?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Walks.FirstOrDefaultAsync(walk => walk.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Walk>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        await db.Walks.AsNoTracking()
            .Where(walk => walk.OwnerId == ownerId)
            .OrderByDescending(walk => walk.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Walk>> ListRequestedAsync(CancellationToken cancellationToken) =>
        await db.Walks.AsNoTracking()
            .Where(walk => walk.Status == WalkStatus.Requested)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Walk>> ListRequestedNearAsync(
        GeoPoint point,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(point);
        double radiusMeters = radiusKm * 1000;

        // PostGIS on the geography column: ST_DWithin uses the GiST index, ST_Distance is measured on the spheroid.
        // xmin is a system column (not in *), but EF needs it for the concurrency token.
        return await db.Walks
            .FromSqlInterpolated($"""
                WITH here AS (
                    SELECT ST_SetSRID(ST_MakePoint({point.Longitude}, {point.Latitude}), 4326)::geography AS point
                )
                SELECT w.*, w.xmin
                FROM walks.walks w, here
                WHERE w.status = 'Requested' AND ST_DWithin(w.pickup_location, here.point, {radiusMeters})
                ORDER BY ST_Distance(w.pickup_location, here.point)
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Walk>> ListByWalkerAsync(string walkerId, CancellationToken cancellationToken) =>
        await db.Walks.AsNoTracking()
            .Where(walk => walk.WalkerId == walkerId)
            .OrderBy(walk => walk.ScheduledFor)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Someone changed the walk after we read it (e.g. another walker accepted it first).
            throw new ConflictException(WalkErrors.NotAvailable, "The walk changed while saving it", exception);
        }
    }
}
