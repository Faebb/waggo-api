using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Identity.Commands.VerifyLoginCode;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.RefreshSession;

/// <summary>
/// RNF-002: every refresh rotates the token. Presenting one already rotated or expired fails and, if it was
/// reuse of an old one, revokes every refresh token of that user (a stolen token was probably used).
/// </summary>
internal sealed class RefreshSessionHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IWalkerProfileRepository walkerProfiles,
    ITokenGenerator tokenGenerator,
    ITokenService tokenService,
    TimeProvider timeProvider)
    : ICommandHandler<RefreshSessionCommand, SessionResponse>
{
    public async Task<WaggoResponse<SessionResponse>> HandleAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<SessionResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        string tokenHash = tokenGenerator.Hash(command.RefreshToken ?? string.Empty);
        RefreshToken? existing = await refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is null)
        {
            response.AddError(IdentityErrors.RefreshTokenInvalid);
            return response;
        }

        if (existing.RevokedAt is not null)
        {
            // Already rotated: this token is being replayed, likely stolen. Cut off the whole family.
            await refreshTokens.RevokeAllForUserAsync(existing.UserId, now, cancellationToken);
            response.AddError(IdentityErrors.RefreshTokenInvalid);
            return response;
        }

        User? user = await users.GetAsync(existing.UserId, cancellationToken);
        if (user is null || !existing.IsActive(now))
        {
            response.AddError(IdentityErrors.RefreshTokenInvalid);
            return response;
        }

        (SessionResponse session, Guid newRefreshTokenId) = await VerifyLoginCodeHandler.IssueSessionAsync(
            user, walkerProfiles, refreshTokens, tokenGenerator, tokenService, now, cancellationToken);

        response.ConcatStacks(existing.Rotate(newRefreshTokenId, now));
        await refreshTokens.SaveChangesAsync(cancellationToken);

        response.Data = session;
        return response;
    }
}
