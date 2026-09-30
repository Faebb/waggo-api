using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Commands.CancelWalk;

/// <summary>RF-007: the owner cancels one of their walks before it starts.</summary>
internal sealed class CancelWalkHandler(IWalkRepository walks, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<CancelWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        CancelWalkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkResponse> response = new();

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        if (walk is null || walk.OwnerId != currentUser.Id)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {command.WalkId} not found for {currentUser.Id}");
        }

        response.ConcatStacks(walk.Cancel(timeProvider.GetUtcNow()));
        if (!response.IsValid)
        {
            return response;
        }

        await walks.SaveChangesAsync(cancellationToken);
        response.Data = WalkResponse.From(walk);
        return response;
    }
}
