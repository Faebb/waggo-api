using Waggo.Domain.Entities.Notifications;

namespace Waggo.Application.Common.Interfaces.Notifications;

/// <summary>Storage of the users' notifications (RF-014).</summary>
public interface INotificationRepository
{
    /// <summary>Saves new notifications right away.</summary>
    Task AddRangeAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken);

    /// <summary>The <paramref name="limit"/> most recent notifications of a user, newest first.</summary>
    Task<IReadOnlyList<Notification>> ListLatestAsync(string userId, int limit, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Marks every unread notification of a user as read at <paramref name="now"/>.</summary>
    Task MarkAllReadAsync(string userId, DateTimeOffset now, CancellationToken cancellationToken);
}
