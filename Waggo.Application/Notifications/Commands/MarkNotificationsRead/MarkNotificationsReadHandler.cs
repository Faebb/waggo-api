using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Domain.Common;

namespace Waggo.Application.Notifications.Commands.MarkNotificationsRead;

/// <summary>RF-014: the user opened the inbox; everything in it is read.</summary>
internal sealed class MarkNotificationsReadHandler(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<MarkNotificationsReadCommand, NotificationsResponse>
{
    public async Task<WaggoResponse<NotificationsResponse>> HandleAsync(
        MarkNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(currentUser.Id, timeProvider.GetUtcNow(), cancellationToken);
        return new WaggoResponse<NotificationsResponse>
        {
            Data = await notifications.ReadAsync(currentUser.Id, cancellationToken),
        };
    }
}
