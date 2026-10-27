using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ChangePassword;

/// <summary>
/// RF-005: a signed-in account changes its password. It must prove it knows the current one, and a wrong guess counts
/// toward the same lockout as a wrong login (a stolen access token must not be a way to guess the password). The
/// session that changes it stays open; the others are closed, and the account's email gets a security notice.
/// </summary>
internal sealed class ChangePasswordHandler(
    IValidator<ChangePasswordCommand> validator,
    IUserRepository users,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ChangePasswordCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        User user = await users.GetAsync(Guid.Parse(currentUser.Id), cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {currentUser.Id} was not found.");
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (user.IsLocked(now))
        {
            response.AddError(IdentityErrors.TooManyAttempts);
            return response;
        }

        if (!passwordHasher.Verify(command.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            user.RegisterFailedLoginAttempt(now);
            await users.SaveChangesAsync(cancellationToken);
            response.AddError(IdentityErrors.CurrentPasswordIncorrect);
            return response;
        }

        if (passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            response.AddError(IdentityErrors.PasswordUnchanged);
            return response;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));
        await users.SaveChangesAsync(cancellationToken);

        await CloseOtherSessionsAsync(user, command.RefreshToken, now, cancellationToken);
        await PasswordChangedNotice.SendAsync(emailSender, user.Email, response, cancellationToken);

        response.Data = true;
        return response;
    }

    private async Task CloseOtherSessionsAsync(
        User user,
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
        if (session is not null && session.UserId == user.Id && session.IsActive(now))
        {
            await refreshTokens.RevokeAllForUserExceptAsync(user.Id, session.Id, now, cancellationToken);
            return;
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);
    }
}
