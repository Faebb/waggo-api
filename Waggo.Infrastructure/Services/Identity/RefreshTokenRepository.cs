using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Identity;

internal sealed class RefreshTokenRepository(WaggoDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAllForUserExceptAsync(
        Guid userId,
        Guid keepTokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        List<RefreshToken> others = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.Id != keepTokenId)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in others)
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<RefreshToken> active = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (RefreshToken token in active)
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
