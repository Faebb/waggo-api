using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Commands.FinishWalk;

/// <summary>RF-008/RF-011: the assigned walker brought the dogs back and finishes the walk.</summary>
internal sealed class FinishWalkHandler(IWalkRepository walks, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<FinishWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        FinishWalkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkResponse> response = new();

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        if (walk is null || walk.WalkerId != currentUser.Id)
        {
            throw new NotFoundException(
                WalkErrors.NotFound,
                $"Walk {command.WalkId} is not assigned to {currentUser.Id}");
        }

        response.ConcatStacks(walk.Finish(timeProvider.GetUtcNow()));
        if (!response.IsValid)
        {
            return response;
        }

        await walks.SaveChangesAsync(cancellationToken);
        response.Data = WalkResponse.From(walk);
        return response;
    }
}
