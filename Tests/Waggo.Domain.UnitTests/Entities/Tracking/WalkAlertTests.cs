using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Enums.Tracking;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Entities.Tracking;

public class WalkAlertTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_walkId = Guid.NewGuid();

    [Fact]
    public void RaiseEmergency_WithDetails_KeepsThem()
    {
        GeoPoint where = GeoPoint.Create(4.64, -74.062).Data;

        WaggoResponse<WalkAlert> result =
            WalkAlert.RaiseEmergency(s_walkId, AlertParty.Walker, " Luna se soltó ", where, s_now);

        WalkAlert alert = result.Data;
        (alert.WalkId, alert.Kind, alert.RaisedBy).ShouldBe((s_walkId, AlertKind.Emergency, AlertParty.Walker));
        alert.Message.ShouldBe("Luna se soltó");
        alert.Location.ShouldBe(where);
        alert.RaisedAt.ShouldBe(s_now);
    }

    [Fact]
    public void RaiseEmergency_BlankMessageAndNoLocation_AreNull()
    {
        WalkAlert alert = WalkAlert.RaiseEmergency(s_walkId, AlertParty.Owner, "  ", null, s_now).Data;

        alert.Message.ShouldBeNull();
        alert.Location.ShouldBeNull();
    }

    [Fact]
    public void RaiseEmergency_MessageLongerThan500_FailsWithInvalidMessage() =>
        WalkAlert.RaiseEmergency(s_walkId, AlertParty.Owner, new string('a', 501), null, s_now)
            .Errors.Single().Code.ShouldBe(AlertErrors.InvalidMessage.Code);
}
