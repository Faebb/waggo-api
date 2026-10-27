using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Identity.Commands.ResetPassword;

/// <summary>
/// RF-001/RF-005: the 6-digit code from the email sets a new password. Every open session is closed, and the
/// account's email gets a security notice.
/// </summary>
internal sealed class ResetPasswordHandler(
    IValidator<ResetPasswordCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        User? user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.EmailConfirmed)
        {
            // Same answer as a wrong code, so the endpoint does not reveal which emails have an account.
            response.AddError(IdentityErrors.ResetCodeInvalid);
            return response;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string codeHash = tokenGenerator.Hash(command.Code ?? string.Empty);
        WaggoResponse<User> reset = user.ResetPassword(codeHash, passwordHasher.Hash(command.NewPassword), now);

        // Saved even when the code was wrong: the failed attempt is what limits guessing.
        await users.SaveChangesAsync(cancellationToken);

        response.ConcatStacks(reset);
        if (!response.IsValid)
        {
            return response;
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);
        await NotifyAsync(user.Email, response, cancellationToken);

        response.Data = true;
        return response;
    }

    private async Task NotifyAsync(string email, WaggoResponse<bool> response, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(
                email,
                PasswordChangedEmail.Subject,
                PasswordChangedEmail.Body,
                cancellationToken);
        }
        catch (ExternalServiceException exception)
        {
            // The password is already changed: a failed notice must not turn that into an error.
            response.AddWarning(
                EmailErrors.SendFailed.Code,
                $"No se pudo enviar el aviso de seguridad: {exception.Message}",
                MessageVisibility.Internal);
        }
    }
}
