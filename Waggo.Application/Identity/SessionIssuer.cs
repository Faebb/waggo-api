using Waggo.Application.Common.Constants;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>Shared by every way of getting a session (password login, external login, refresh): computes the
/// current roles and issues a fresh access + refresh token (ADR-016).</summary>
internal static class SessionIssuer
{
    public static async Task<(SessionResponse Session, Guid RefreshTokenId)> IssueSessionAsync(
        User user,
        IWalkerProfileRepository walkerProfiles,
        IRefreshTokenRepository refreshTokens,
        ITokenGenerator tokenGenerator,
        ITokenService tokenService,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        List<string> roles = [WaggoRoles.Owner];
        if ((await walkerProfiles.GetByUserAsync(user.Id.ToString(), cancellationToken))?.IsVerified == true)
        {
            roles.Add(WaggoRoles.Walker);
        }

        if (user.IsAdmin)
        {
            roles.Add(WaggoRoles.Admin);
        }

        (string accessToken, int expiresInSeconds) = tokenService.IssueAccessToken(user.Id, roles);

        string rawRefreshToken = tokenGenerator.GenerateToken();
        RefreshToken refreshToken = RefreshToken.Create(user.Id, tokenGenerator.Hash(rawRefreshToken), now);
        await refreshTokens.AddAsync(refreshToken, cancellationToken);

        return (new SessionResponse(accessToken, rawRefreshToken, expiresInSeconds), refreshToken.Id);
    }
}
