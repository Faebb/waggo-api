using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Errors.Walkers;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walkers.Commands.ApproveWalker;

/// <summary>RF-003: an admin approves a pending walker.</summary>
internal sealed class ApproveWalkerHandler(
    IWalkerProfileRepository profiles,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<ApproveWalkerCommand, WalkerProfileResponse>
{
    public async Task<WaggoResponse<WalkerProfileResponse>> HandleAsync(
        ApproveWalkerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkerProfileResponse> response = new();

        WalkerProfile profile = await profiles.GetAsync(command.ProfileId, cancellationToken)
            ?? throw new NotFoundException(WalkerErrors.NotFound, $"Walker profile {command.ProfileId} does not exist");

        response.ConcatStacks(profile.Approve(currentUser.Id, timeProvider.GetUtcNow()));
        if (!response.IsValid)
        {
            return response;
        }

        await profiles.SaveChangesAsync(cancellationToken);
        response.Data = WalkerProfileResponse.From(profile);
        return response;
    }
}
