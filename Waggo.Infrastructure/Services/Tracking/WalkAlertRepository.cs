using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Domain.Entities.Tracking;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Tracking;

internal sealed class WalkAlertRepository(WaggoDbContext db) : IWalkAlertRepository
{
    public async Task AddAsync(WalkAlert alert, CancellationToken cancellationToken)
    {
        db.WalkAlerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WalkAlert>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken) =>
        await db.WalkAlerts.AsNoTracking()
            .Where(alert => alert.WalkId == walkId)
            .OrderByDescending(alert => alert.RaisedAt)
            .ToListAsync(cancellationToken);
}
