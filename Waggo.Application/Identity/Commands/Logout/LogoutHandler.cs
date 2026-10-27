using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Commands.Logout;

/// <summary>
/// RF-005, RNF-002: signing out revokes the refresh token, so it cannot open another session. It answers the same
/// for a token that is unknown, expired or already revoked: holding the token is the proof, and nothing is revealed.
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
        WaggoResponse<bool> response = new() { Data = true };

        string tokenHash = tokenGenerator.Hash(command.RefreshToken ?? string.Empty);
        RefreshToken? token = await refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null || token.RevokedAt is not null)
        {
            return response;
        }

        token.Revoke(timeProvider.GetUtcNow());
        await refreshTokens.SaveChangesAsync(cancellationToken);
        return response;
    }
}
