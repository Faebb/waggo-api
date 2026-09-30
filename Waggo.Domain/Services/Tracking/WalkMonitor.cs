using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Enums.Tracking;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.Services.Tracking;

/// <summary>
/// Watches a walk from its route and tells which alerts the platform must raise (RF-009 geofence, RF-010 stop).
/// Pure: it receives the route so far and the new batch, and returns the alerts; the caller saves them.
/// </summary>
public static class WalkMonitor
{
    /// <summary>Radius of the agreed zone around the pickup point. Provisional (PO question).</summary>
    public const double GeofenceRadiusKm = 1.5;

    /// <summary>
    /// A walker who stays this close for <see cref="StopMinutes"/> is considered stopped. Provisional.
    /// </summary>
    public const double StopRadiusKm = 0.03;

    public const int StopMinutes = 10;

    public static IReadOnlyList<WalkAlert> Check(
        Guid walkId,
        GeoPoint pickup,
        IReadOnlyList<TrackPoint> previous,
        IReadOnlyList<TrackPoint> incoming,
        DateTimeOffset? lastAnomalyAt)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(incoming);

        List<WalkAlert> alerts = [];
        if (incoming.Count == 0)
        {
            return alerts;
        }

        TrackPoint[] before = [.. previous.OrderBy(point => point.RecordedAt)];
        TrackPoint[] batch = [.. incoming.OrderBy(point => point.RecordedAt)];

        // RF-009: one alert each time the walk crosses the border outwards. A walk starts at the pickup (inside).
        bool wasInside = before.Length == 0 || IsInside(pickup, before[^1]);
        foreach (TrackPoint point in batch)
        {
            bool inside = IsInside(pickup, point);
            if (wasInside && !inside)
            {
                alerts.Add(WalkAlert.RaiseAutomatic(walkId, AlertKind.Geofence, point.Location, point.RecordedAt));
            }

            wasInside = inside;
        }

        // RF-010: the latest positions stay within a few meters of the last one for long enough.
        TrackPoint[] route = [.. before, .. batch];
        TrackPoint last = route[^1];
        DateTimeOffset stoppedSince = last.RecordedAt;
        for (int i = route.Length - 1; i >= 0 && route[i].Location.DistanceKmTo(last.Location) <= StopRadiusKm; i--)
        {
            stoppedSince = route[i].RecordedAt;
        }

        bool stoppedLongEnough = last.RecordedAt - stoppedSince >= TimeSpan.FromMinutes(StopMinutes);
        bool alreadyWarned = lastAnomalyAt is { } warned && warned >= stoppedSince;
        if (stoppedLongEnough && !alreadyWarned)
        {
            alerts.Add(WalkAlert.RaiseAutomatic(walkId, AlertKind.Anomaly, last.Location, last.RecordedAt));
        }

        return alerts;
    }

    private static bool IsInside(GeoPoint pickup, TrackPoint point) =>
        pickup.DistanceKmTo(point.Location) <= GeofenceRadiusKm;
}
