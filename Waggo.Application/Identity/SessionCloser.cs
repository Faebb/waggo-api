using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>Closes the sessions of an account after a sensitive change, keeping only the one that made it.</summary>
internal static class SessionCloser
{
    public static async Task CloseOthersAsync(
        IRefreshTokenRepository refreshTokens,
        ITokenGenerator tokenGenerator,
        Guid userId,
        string? sessionRefreshToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RefreshToken? session = null;
        if (!string.IsNullOrEmpty(sessionRefreshToken))
        {
            session = await refreshTokens.GetByTokenHashAsync(tokenGenerator.Hash(sessionRefreshToken), cancellationToken);
        }

        // Only this account's own active session may be kept: anything else closes every session.
        if (session is not null && session.UserId == userId && session.IsActive(now))
        {
            await refreshTokens.RevokeAllForUserExceptAsync(userId, session.Id, now, cancellationToken);
            return;
        }

        await refreshTokens.RevokeAllForUserAsync(userId, now, cancellationToken);
    }
}
