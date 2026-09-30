namespace Waggo.Domain.Enums.Tracking;

/// <summary>Why an alert was raised during a walk.</summary>
public enum AlertKind
{
    /// <summary>The owner or the walker pressed the emergency button (RF-012).</summary>
    Emergency = 1,

    /// <summary>The walk left the agreed zone around the pickup (RF-009). Raised by the platform.</summary>
    Geofence = 2,

    /// <summary>The walker has not moved for a while (RF-010). Raised by the platform.</summary>
    Anomaly = 3,
}
