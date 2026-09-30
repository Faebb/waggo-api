using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Entities.Walks;
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

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
