using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Commands.Logout;

/// <summary>
/// Ends a session by revoking its refresh token (RNF-002). Always succeeds: an unknown, expired or already revoked
/// token leaves nothing to close, and answering differently would let anyone probe which tokens exist.
/// </summary>
internal sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenGenerator tokenGenerator,
    TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        string tokenHash = tokenGenerator.Hash(command.RefreshToken ?? string.Empty);
        RefreshToken? existing = await refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.Revoke(timeProvider.GetUtcNow());
            await refreshTokens.SaveChangesAsync(cancellationToken);
        }

        response.Data = true;
        return response;
    }
}
