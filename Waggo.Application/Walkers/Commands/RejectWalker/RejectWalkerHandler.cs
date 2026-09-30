using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Errors.Walkers;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walkers.Commands.RejectWalker;

/// <summary>RF-003: an admin rejects a pending walker, saying why.</summary>
internal sealed class RejectWalkerHandler(
    IWalkerProfileRepository profiles,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RejectWalkerCommand, WalkerProfileResponse>
{
    public async Task<WaggoResponse<WalkerProfileResponse>> HandleAsync(
        RejectWalkerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkerProfileResponse> response = new();

        WalkerProfile profile = await profiles.GetAsync(command.ProfileId, cancellationToken)
            ?? throw new NotFoundException(WalkerErrors.NotFound, $"Walker profile {command.ProfileId} does not exist");

        response.ConcatStacks(profile.Reject(currentUser.Id, command.Reason, timeProvider.GetUtcNow()));
        if (!response.IsValid)
        {
            return response;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        response.Data = WalkerProfileResponse.From(profile);
        return response;
    }
}
