using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.Login;

/// <summary>
/// RF-005: a right password and a confirmed account issue a session right away (ADR-017 — no second factor; a
/// Google/Microsoft account brings its own). Wrong attempts are counted and lock the account for a while
/// (RNF-002 brute force).
/// </summary>
internal sealed class LoginHandler(
    IUserRepository users,
    IWalkerProfileRepository walkerProfiles,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenService tokenService,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, SessionResponse>
{
    public async Task<WaggoResponse<SessionResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<SessionResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        User? user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            response.AddError(IdentityErrors.InvalidCredentials);
            return response;
        }

        if (user.IsLocked(now))
        {
            response.AddError(IdentityErrors.TooManyAttempts);
            return response;
        }

        if (!passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            user.RegisterFailedLoginAttempt(now);
            await users.SaveChangesAsync(cancellationToken);
            response.AddError(IdentityErrors.InvalidCredentials);
            return response;
        }

        if (!user.EmailConfirmed)
        {
            response.AddError(IdentityErrors.AccountNotConfirmed);
            return response;
        }

        user.ResetFailedLoginAttempts();
        await users.SaveChangesAsync(cancellationToken);

        (SessionResponse session, _) = await SessionIssuer.IssueSessionAsync(
            user, walkerProfiles, refreshTokens, tokenGenerator, tokenService, now, cancellationToken);
        response.Data = session;
        return response;
    }
}
