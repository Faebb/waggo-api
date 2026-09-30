using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Domain.Common;

namespace Waggo.Application.Notifications.Queries.ListMyNotifications;

/// <summary>RF-014: the current user's inbox.</summary>
internal sealed class ListMyNotificationsHandler(INotificationRepository notifications, ICurrentUser currentUser)
    : IQueryHandler<ListMyNotificationsQuery, NotificationsResponse>
{
    public async Task<WaggoResponse<NotificationsResponse>> HandleAsync(
        ListMyNotificationsQuery query,
        CancellationToken cancellationToken) =>
        new() { Data = await notifications.ReadAsync(currentUser.Id, cancellationToken) };
}
