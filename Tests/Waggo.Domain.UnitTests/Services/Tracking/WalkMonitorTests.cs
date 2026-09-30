using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Enums.Tracking;
using Waggo.Domain.Services.Tracking;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Services.Tracking;

public class WalkMonitorTests
{
    private static readonly DateTimeOffset s_start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_walkId = Guid.NewGuid();
    private static readonly GeoPoint s_pickup = GeoPoint.Create(4.6361, -74.0645).Data;

    private static TrackPoint At(double latitude, double minute, double longitude = -74.0645) =>
        TrackPoint.Record(s_walkId, GeoPoint.Create(latitude, longitude).Data, s_start.AddMinutes(minute));

    private static IReadOnlyList<WalkAlert> Check(
        TrackPoint[] previous,
        TrackPoint[] incoming,
        DateTimeOffset? lastAnomalyAt = null) =>
        WalkMonitor.Check(s_walkId, s_pickup, previous, incoming, lastAnomalyAt);

    [Fact]
    public void Check_NormalWalkInsideTheZone_RaisesNothing() =>
        Check([At(4.6361, 0), At(4.6400, 3)], [At(4.6440, 6), At(4.6480, 9)]).ShouldBeEmpty();

    [Fact]
    public void Check_LeavingTheZone_RaisesAGeofenceAlertWhereItLeft()
    {
        IReadOnlyList<WalkAlert> alerts = Check([At(4.6400, 0)], [At(4.6500, 3), At(4.6631, 6)]);

        WalkAlert alert = alerts.Single();
        alert.Kind.ShouldBe(AlertKind.Geofence);
        alert.RaisedBy.ShouldBeNull();
        alert.Location!.Latitude.ShouldBe(4.6631);
        alert.RaisedAt.ShouldBe(s_start.AddMinutes(6));
    }

    [Fact]
    public void Check_StillOutside_RaisesNothingNew() =>
        Check([At(4.6400, 0), At(4.6631, 3)], [At(4.6640, 6)]).ShouldBeEmpty();

    [Fact]
    public void Check_BackInsideAndOutAgain_RaisesAnotherGeofenceAlert() =>
        Check([At(4.6631, 3)], [At(4.6400, 6), At(4.6631, 9)])
            .Count(alert => alert.Kind == AlertKind.Geofence).ShouldBe(1);

    [Fact]
    public void Check_StillForElevenMinutes_RaisesOneAnomaly()
    {
        IReadOnlyList<WalkAlert> alerts = Check(
            [At(4.6400, 0), At(4.6401, 4)],
            [At(4.6400, 8), At(4.6401, 11)]);

        WalkAlert alert = alerts.Single();
        alert.Kind.ShouldBe(AlertKind.Anomaly);
        alert.RaisedAt.ShouldBe(s_start.AddMinutes(11));
    }

    [Fact]
    public void Check_StillStoppedAfterTheAnomalyWasRaised_RaisesNothingNew() =>
        Check(
            [At(4.6400, 0), At(4.6401, 4), At(4.6400, 8), At(4.6401, 11)],
            [At(4.6400, 14)],
            lastAnomalyAt: s_start.AddMinutes(11)).ShouldBeEmpty();

    [Fact]
    public void Check_StillForOnlyEightMinutes_RaisesNothing() =>
        Check([At(4.6400, 0)], [At(4.6401, 4), At(4.6400, 8)]).ShouldBeEmpty();

    [Fact]
    public void Check_StoppedAgainAfterMoving_RaisesAnotherAnomaly() =>
        Check(
            [At(4.6400, 0), At(4.6400, 11), At(4.6450, 14)],
            [At(4.6451, 20), At(4.6450, 25)],
            lastAnomalyAt: s_start.AddMinutes(11)).Single().Kind.ShouldBe(AlertKind.Anomaly);
}
