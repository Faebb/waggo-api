using Waggo.Application.Common.Constants;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.VerifyLoginCode;

/// <summary>RF-005, step 2: the right code turns the challenge into a real session (access + refresh token).</summary>
internal sealed class VerifyLoginCodeHandler(
    ILoginChallengeRepository challenges,
    IUserRepository users,
    IWalkerProfileRepository walkerProfiles,
    IRefreshTokenRepository refreshTokens,
    ITokenGenerator tokenGenerator,
    ITokenService tokenService,
    TimeProvider timeProvider)
    : ICommandHandler<VerifyLoginCodeCommand, SessionResponse>
{
    public async Task<WaggoResponse<SessionResponse>> HandleAsync(
        VerifyLoginCodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<SessionResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        LoginChallenge? challenge = await challenges.GetAsync(command.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            response.AddError(IdentityErrors.ChallengeNotFound);
            return response;
        }

        WaggoResponse<LoginChallenge> verified =
            challenge.Verify(tokenGenerator.Hash(command.Code ?? string.Empty), now);
        response.ConcatStacks(verified);
        await challenges.SaveChangesAsync(cancellationToken);
        if (!response.IsValid)
        {
            return response;
        }

        User user = await users.GetAsync(challenge.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {challenge.UserId} of a verified challenge was not found.");

        (SessionResponse session, _) = await IssueSessionAsync(
            user, walkerProfiles, refreshTokens, tokenGenerator, tokenService, now, cancellationToken);
        response.Data = session;
        return response;
    }

    /// <summary>Shared by login and refresh: computes the current roles and issues a fresh access + refresh token.</summary>
    internal static async Task<(SessionResponse Session, Guid RefreshTokenId)> IssueSessionAsync(
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
