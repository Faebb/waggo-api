using Waggo.Domain.Entities.Tracking;

namespace Waggo.Domain.Services.Tracking;

/// <summary>Summary of a walk's route (RF-011): how far and how long.</summary>
public static class RouteMetrics
{
    /// <summary>Positions per request: the phone sends them in batches (RNF-007), e.g. after losing signal.</summary>
    public const int MaxPointsPerBatch = 100;

    /// <summary>Sum of the straight-line legs between the points, in the order they were recorded.</summary>
    public static double DistanceKm(IEnumerable<TrackPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        TrackPoint[] ordered = [.. points.OrderBy(point => point.RecordedAt)];
        double total = 0;
        for (int i = 1; i < ordered.Length; i++)
        {
            total += ordered[i - 1].Location.DistanceKmTo(ordered[i].Location);
        }

        return total;
    }

    /// <summary>Whole minutes from the start to the finish, or to now while the walk goes on.</summary>
    public static int ElapsedMinutes(DateTimeOffset? startedAt, DateTimeOffset? finishedAt, DateTimeOffset now) =>
        startedAt is null ? 0 : (int)Math.Floor(((finishedAt ?? now) - startedAt.Value).TotalMinutes);
}
