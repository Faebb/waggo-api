using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Commands.AcceptWalk;

/// <summary>
/// RF-007: the current walker takes an open request. If two walkers accept at the same time, the repository
/// detects it when saving and the second one gets <c>Walks.NotAvailable</c>.
/// </summary>
internal sealed class AcceptWalkHandler(
    IWalkRepository walks,
    IWalkerProfileRepository walkerProfiles,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<AcceptWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        AcceptWalkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkResponse> response = new();

        await walkerProfiles.EnsureVerifiedAsync(currentUser.Id, cancellationToken);

        Walk walk = await walks.GetAsync(command.WalkId, cancellationToken)
            ?? throw new NotFoundException(WalkErrors.NotFound, $"Walk {command.WalkId} does not exist");

        response.ConcatStacks(walk.Accept(currentUser.Id, timeProvider.GetUtcNow()));
        if (!response.IsValid)
        {
            return response;
        }

        await walks.SaveChangesAsync(cancellationToken);
        response.Data = WalkResponse.From(walk);
        return response;
    }
}
