namespace Waggo.Domain.Enums.Notifications;

/// <summary>The moments of a walk that notify the other side (RF-014).</summary>
public enum NotificationKind
{
    WalkAccepted = 1,
    WalkStarted = 2,
    WalkFinished = 3,
    WalkPaid = 4,
    WalkCancelled = 5,
    Emergency = 6,
    Geofence = 7,
    Anomaly = 8,
}
