using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ConfirmEmail;

/// <summary>RF-001: activates the account once the owner of the inbox presents the confirmation token.</summary>
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

        string tokenHash = tokenGenerator.Hash(command.Token ?? string.Empty);
        User? user = await users.GetByConfirmationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
        {
            response.AddError(IdentityErrors.ConfirmationTokenInvalid);
            return response;
        }

        WaggoResponse<User> confirmed = user.Confirm(tokenHash, timeProvider.GetUtcNow());
        response.ConcatStacks(confirmed);
        if (!response.IsValid)
        {
            return response;
        }

        await users.SaveChangesAsync(cancellationToken);
        response.Data = true;
        return response;
    }
}
