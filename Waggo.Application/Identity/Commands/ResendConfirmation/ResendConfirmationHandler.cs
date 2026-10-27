using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Commands.ResendConfirmation;

/// <summary>
/// RF-001: a new confirmation code when the first one did not arrive, expired or ran out of attempts. It answers the
/// same whether or not the email has an account, or the wait between emails has not passed, so it reveals nothing.
/// </summary>
internal sealed class ResendConfirmationHandler(
    IUserRepository users,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ResendConfirmationCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(
        ResendConfirmationCommand command,
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
        if (!user.TryIssueConfirmationCode(tokenGenerator.Hash(code), timeProvider.GetUtcNow()))
        {
            return response;
        }

        // Email first: if the provider fails nothing is saved and the previous code keeps working.
        await emailSender.SendAsync(user.Email, ConfirmationEmail.Subject, ConfirmationEmail.Body(code), cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return response;
    }
}
