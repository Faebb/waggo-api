using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ConfirmEmail;

/// <summary>RF-001: activates the account once the owner of the inbox types the 6-digit code from the email.</summary>
internal sealed class ConfirmEmailHandler(
    IUserRepository users,
    ITokenGenerator tokenGenerator,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmEmailCommand, bool>
{
    public async Task<WaggoResponse<bool>> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<bool> response = new();

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        User? user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || user.EmailConfirmed)
        {
            // Same answer as a wrong code, so the endpoint does not reveal which emails have an account.
            response.AddError(IdentityErrors.ConfirmationTokenInvalid);
            return response;
        }

        string codeHash = tokenGenerator.Hash(command.Code ?? string.Empty);
        WaggoResponse<User> confirmed = user.Confirm(codeHash, timeProvider.GetUtcNow());

        // Saved even when the code was wrong: the failed attempt is what limits guessing.
        await users.SaveChangesAsync(cancellationToken);

        response.ConcatStacks(confirmed);
        if (!response.IsValid)
        {
            return response;
        }

        response.Data = true;
        return response;
    }
}
