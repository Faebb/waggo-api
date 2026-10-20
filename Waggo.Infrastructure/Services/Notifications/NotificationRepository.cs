using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Domain.Entities.Notifications;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Notifications;

internal sealed class NotificationRepository(WaggoDbContext db) : INotificationRepository
{
    public async Task AddRangeAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken)
    {
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListLatestAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken) =>
        await db.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(
            notification => notification.UserId == userId && notification.ReadAt == null,
            cancellationToken);

    public Task MarkAllReadAsync(string userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.ReadAt, now),
                cancellationToken);
}
