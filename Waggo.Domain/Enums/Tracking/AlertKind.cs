namespace Waggo.Domain.Enums.Tracking;

/// <summary>Why an alert was raised during a walk. Geofences (RF-009) and anomalies (RF-010) come next.</summary>
public enum AlertKind
{
    /// <summary>The owner or the walker pressed the emergency button (RF-012).</summary>
    Emergency = 1,
}
