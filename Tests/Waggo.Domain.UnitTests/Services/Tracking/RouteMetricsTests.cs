using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Services.Tracking;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Services.Tracking;

public class RouteMetricsTests
{
    private static readonly DateTimeOffset s_start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_walkId = Guid.NewGuid();

    private static TrackPoint At(double latitude, int minute) =>
        TrackPoint.Record(s_walkId, GeoPoint.Create(latitude, -74.0645).Data, s_start.AddMinutes(minute));

    [Fact]
    public void DistanceKm_AddsEveryLegInTheOrderTheyWereRecorded()
    {
        // ~0.49 km + ~0.50 km, given out of order.
        TrackPoint[] points = [At(4.6450, 2), At(4.6361, 0), At(4.6405, 1)];

        RouteMetrics.DistanceKm(points).ShouldBe(0.99, 0.02);
    }

    [Fact]
    public void DistanceKm_LessThanTwoPoints_IsZero()
    {
        RouteMetrics.DistanceKm([]).ShouldBe(0);
        RouteMetrics.DistanceKm([At(4.6361, 0)]).ShouldBe(0);
    }

    [Fact]
    public void ElapsedMinutes_FinishedWalk_IsFromStartToFinish() =>
        RouteMetrics.ElapsedMinutes(s_start, s_start.AddMinutes(47.6), s_start.AddHours(3)).ShouldBe(47);

    [Fact]
    public void ElapsedMinutes_WalkInProgress_IsFromStartToNow() =>
        RouteMetrics.ElapsedMinutes(s_start, null, s_start.AddMinutes(18)).ShouldBe(18);

    [Fact]
    public void ElapsedMinutes_NotStarted_IsZero() =>
        RouteMetrics.ElapsedMinutes(null, null, s_start).ShouldBe(0);
}
