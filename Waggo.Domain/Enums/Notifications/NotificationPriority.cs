namespace Waggo.Domain.Enums.Notifications;

/// <summary>High for emergencies and automatic alerts (RF-009, RF-010, RF-012): they must be seen right away.</summary>
public enum NotificationPriority
{
    Normal = 1,
    High = 2,
}
