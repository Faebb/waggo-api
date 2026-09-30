using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Walkers;

internal sealed class WalkerProfileRepository(WaggoDbContext db) : IWalkerProfileRepository
{
    public async Task AddAsync(WalkerProfile profile, CancellationToken cancellationToken)
    {
        db.WalkerProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<WalkerProfile?> GetByUserAsync(string userId, CancellationToken cancellationToken) =>
        db.WalkerProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public Task<WalkerProfile?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.WalkerProfiles.FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WalkerProfile>> ListByStatusAsync(
        VerificationStatus? status,
        CancellationToken cancellationToken) =>
        await db.WalkerProfiles.AsNoTracking()
            .Where(profile => status == null || profile.Status == status)
            .OrderBy(profile => profile.RegisteredAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
