using Waggo.Domain.Entities.Notifications;

namespace Waggo.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid WalkId,
    string RecipientParty,
    string Kind,
    string Priority,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt)
{
    public static NotificationResponse From(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return new NotificationResponse(
            notification.Id,
            notification.WalkId,
            notification.RecipientParty.ToString(),
            notification.Kind.ToString(),
            notification.Priority.ToString(),
            notification.Title,
            notification.Body,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
