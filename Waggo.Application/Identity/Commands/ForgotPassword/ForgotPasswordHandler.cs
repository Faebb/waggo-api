using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Commands.ForgotPassword;

/// <summary>
/// RF-001/RF-005: emails a 6-digit code to set a new password. It answers the same whether or not the email has a
/// confirmed account, or the wait between emails has not passed, so it reveals nothing about who has an account.
/// </summary>
internal sealed class ForgotPasswordHandler(
    IUserRepository users,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ForgotPasswordCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new() { Data = true };

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        User? user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return response;
        }

        string code = tokenGenerator.GenerateNumericCode();
        if (!user.TryIssuePasswordResetCode(tokenGenerator.Hash(code), timeProvider.GetUtcNow()))
        {
            return response;
        }

        // Email first: if the provider fails nothing is saved and the previous code keeps working.
        await emailSender.SendAsync(
            user.Email,
            PasswordResetEmail.Subject,
            PasswordResetEmail.Body(code),
            cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return response;
    }
}
