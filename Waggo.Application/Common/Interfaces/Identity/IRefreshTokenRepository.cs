using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Storage of rotating sessions (RNF-002).</summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Reuse of an already-rotated token is a stolen-token signal: the whole family is cut off.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Closes every session of the user but the one that is changing the password.</summary>
    Task RevokeAllForUserExceptAsync(
        Guid userId,
        Guid keepTokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Persists changes to an entity already tracked from a previous <c>Get</c> call.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
