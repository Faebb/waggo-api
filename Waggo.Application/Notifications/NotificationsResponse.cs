namespace Waggo.Application.Notifications;

/// <summary>The inbox: how many are unread and the most recent notifications.</summary>
public sealed record NotificationsResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);
