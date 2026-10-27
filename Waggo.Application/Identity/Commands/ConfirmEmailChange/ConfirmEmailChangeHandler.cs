using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ConfirmEmailChange;

/// <summary>
/// RF-005: the code typed from the new inbox applies the new email. The session that confirms it stays open, the
/// others are closed, and the OLD address gets a security notice (it is the one that could be under attack).
/// </summary>
internal sealed class ConfirmEmailChangeHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmEmailChangeCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        ConfirmEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        User user = await users.GetAsync(Guid.Parse(currentUser.Id), cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {currentUser.Id} was not found.");
        DateTimeOffset now = timeProvider.GetUtcNow();
        string oldEmail = user.Email;

        // Someone may have registered that address between the request and the code.
        if (user.PendingEmail is not null && await users.ExistsByEmailAsync(user.PendingEmail, cancellationToken))
        {
            response.AddError(IdentityErrors.EmailAlreadyRegistered);
            return response;
        }

        WaggoResponse<User> changed = user.ConfirmEmailChange(tokenGenerator.Hash(command.Code ?? string.Empty), now);

        // Saved even when the code was wrong: the failed attempt is what limits guessing.
        await users.SaveChangesAsync(cancellationToken);

        response.ConcatStacks(changed);
        if (!response.IsValid)
        {
            return response;
        }

        await SessionCloser.CloseOthersAsync(
            refreshTokens, tokenGenerator, user.Id, command.RefreshToken, now, cancellationToken);
        await SecurityNotice.SendAsync(
            emailSender,
            oldEmail,
            EmailChangeEmail.NoticeSubject,
            EmailChangeEmail.NoticeBody(user.Email),
            response,
            cancellationToken);

        response.Data = true;
        return response;
    }
}
