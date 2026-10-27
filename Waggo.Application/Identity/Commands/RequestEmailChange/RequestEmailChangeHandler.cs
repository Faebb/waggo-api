using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.RequestEmailChange;

/// <summary>
/// RF-005: the signed-in account asks to move to another email. It must prove it knows the password (a wrong guess
/// counts toward the login lockout), and a 6-digit code goes to the NEW inbox; the email only changes once the code
/// is typed (<c>ConfirmEmailChangeHandler</c>).
/// </summary>
internal sealed class RequestEmailChangeHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<RequestEmailChangeCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        RequestEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        User user = await users.GetAsync(Guid.Parse(currentUser.Id), cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {currentUser.Id} was not found.");
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (user.IsLocked(now))
        {
            response.AddError(IdentityErrors.TooManyAttempts);
            return response;
        }

        if (!passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            user.RegisterFailedLoginAttempt(now);
            await users.SaveChangesAsync(cancellationToken);
            response.AddError(IdentityErrors.CurrentPasswordIncorrect);
            return response;
        }

        string newEmail = command.NewEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        if (newEmail == user.Email)
        {
            response.AddError(IdentityErrors.EmailUnchanged);
            return response;
        }

        if (await users.ExistsByEmailAsync(newEmail, cancellationToken))
        {
            response.AddError(IdentityErrors.EmailAlreadyRegistered);
            return response;
        }

        string code = tokenGenerator.GenerateNumericCode();

        // Email first: if the provider fails the account is left untouched and keeps its current email.
        await emailSender.SendAsync(
            newEmail,
            EmailChangeEmail.CodeSubject,
            EmailChangeEmail.CodeBody(code),
            cancellationToken);

        user.ResetFailedLoginAttempts();
        user.RequestEmailChange(newEmail, tokenGenerator.Hash(code), now);
        await users.SaveChangesAsync(cancellationToken);

        response.Data = true;
        return response;
    }
}
