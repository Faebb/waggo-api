using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Notifications;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Notifications;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Commands.StartWalk;

/// <summary>RF-008: the assigned walker picked the dogs up and starts the walk.</summary>
internal sealed class StartWalkHandler(
    IWalkRepository walks,
    INotificationRepository notifications,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<StartWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        StartWalkCommand command,
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

        DateTimeOffset now = timeProvider.GetUtcNow();
        response.ConcatStacks(walk.Start(now));
        if (!response.IsValid)
        {
            return response;
        }

        await walks.SaveChangesAsync(cancellationToken);
        await notifications.AddRangeAsync(
            [Notification.ForWalk(walk, WalkParty.Owner, NotificationKind.WalkStarted, now)],
            cancellationToken);
        response.Data = WalkResponse.From(walk);
        return response;
    }
}
