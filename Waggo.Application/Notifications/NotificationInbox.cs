using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Domain.Entities.Notifications;

namespace Waggo.Application.Notifications;

/// <summary>Reads a user's inbox. Shared by the list query and the "mark as read" command.</summary>
internal static class NotificationInbox
{
    /// <summary>How many notifications the inbox shows (provisional).</summary>
    public const int Size = 50;

    public static async Task<NotificationsResponse> ReadAsync(
        this INotificationRepository notifications,
        string userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Notification> latest = await notifications.ListLatestAsync(userId, Size, cancellationToken);
        int unread = await notifications.CountUnreadAsync(userId, cancellationToken);
        return new NotificationsResponse(unread, [.. latest.Select(NotificationResponse.From)]);
    }
}
