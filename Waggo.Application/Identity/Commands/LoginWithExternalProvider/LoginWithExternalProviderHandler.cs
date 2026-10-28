using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.LoginWithExternalProvider;

/// <summary>
/// RF-001 + RF-005 with Google or Microsoft (ADR-017). The account is found by the provider's own user id; only the
/// first time, the provider's verified email links it to an existing account or creates a new, already confirmed one.
/// The session is the same one a password login issues (ADR-016).
/// </summary>
internal sealed class LoginWithExternalProviderHandler(
    IEnumerable<IExternalIdentityProvider> providers,
    IExternalLoginRepository externalLogins,
    IUserRepository users,
    IWalkerProfileRepository walkerProfiles,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenService tokenService,
    TimeProvider timeProvider)
    : ICommandHandler<LoginWithExternalProviderCommand, SessionResponse>
{
    public async Task<WaggoResponse<SessionResponse>> HandleAsync(
        LoginWithExternalProviderCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<SessionResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        IExternalIdentityProvider? provider = providers.FirstOrDefault(p => p.Provider == command.Provider);
        if (provider is null)
        {
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        WaggoResponse<ExternalIdentity> validation = await provider.ValidateAsync(command.IdToken, cancellationToken);
        response.ConcatStacks(validation);
        if (!response.IsValid)
        {
            return response;
        }

        ExternalIdentity identity = validation.Data;
        User? user;
        ExternalLogin? link = await externalLogins.GetAsync(command.Provider, identity.ProviderUserId, cancellationToken);
        if (link is not null)
        {
            user = await users.GetAsync(link.UserId, cancellationToken);
            if (user is null)
            {
                response.AddError(IdentityErrors.ExternalTokenInvalid);
                return response;
            }
        }
        else
        {
            WaggoResponse<User> linked = await LinkOrCreateAsync(command, identity, now, cancellationToken);
            response.ConcatStacks(linked);
            if (!response.IsValid)
            {
                return response;
            }

            user = linked.Data;
        }

        (SessionResponse session, _) = await SessionIssuer.IssueSessionAsync(
            user, walkerProfiles, refreshTokens, tokenGenerator, tokenService, now, cancellationToken);
        response.Data = session;
        return response;
    }

    /// <summary>First sign-in with this provider account: link it to the account with that email, or create one.</summary>
    private async Task<WaggoResponse<User>> LinkOrCreateAsync(
        LoginWithExternalProviderCommand command,
        ExternalIdentity identity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        WaggoResponse<User> response = new();
        if (!identity.EmailVerified)
        {
            response.AddError(IdentityErrors.ExternalEmailNotVerified);
            return response;
        }

        string email = identity.Email.Trim().ToLowerInvariant();
        string unusablePasswordHash = passwordHasher.Hash(tokenGenerator.GenerateToken());

        User? user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            WaggoResponse<User> created = User.RegisterExternal(email, unusablePasswordHash, now);
            response.ConcatStacks(created);
            if (!response.IsValid)
            {
                return response;
            }

            user = created.Data;
            await users.AddAsync(user, cancellationToken);
        }
        else if (!user.EmailConfirmed)
        {
            user.ConfirmByExternalProvider(unusablePasswordHash);
            await users.SaveChangesAsync(cancellationToken);
        }

        await externalLogins.AddAsync(
            ExternalLogin.Create(user.Id, command.Provider, identity.ProviderUserId, now), cancellationToken);
        response.Data = user;
        return response;
    }
}
