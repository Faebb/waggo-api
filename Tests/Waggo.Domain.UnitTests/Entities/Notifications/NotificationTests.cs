using Waggo.Domain.Entities.Notifications;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Notifications;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Entities.Notifications;

public class NotificationTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static Walk RequestedWalk() => Walk.Request(
        "owner-1",
        [Guid.NewGuid()],
        WalkType.Individual,
        WalkDuration.Create(60).Data,
        "Cra 7 # 45-10, Bogotá",
        GeoPoint.Create(4.6361, -74.0645).Data,
        null,
        null,
        new FareBreakdown(Money.Of(23000m, "COP"), Money.Of(4600m, "COP"), Money.Of(18400m, "COP")),
        s_now).Data;

    [Fact]
    public void ForWalk_Accepted_GoesToTheOwnerWithItsText()
    {
        Walk walk = RequestedWalk();
        walk.Accept("walker-1", s_now);

        Notification notification = Notification.ForWalk(walk, WalkParty.Owner, NotificationKind.WalkAccepted, s_now);

        (notification.UserId, notification.WalkId, notification.RecipientParty)
            .ShouldBe(("owner-1", walk.Id, WalkParty.Owner));
        (notification.Kind, notification.Priority).ShouldBe((NotificationKind.WalkAccepted, NotificationPriority.Normal));
        notification.Title.ShouldBe("Tu paseo fue aceptado");
        notification.Body.ShouldNotBeNullOrWhiteSpace();
        (notification.CreatedAt, notification.ReadAt).ShouldBe((s_now, null));
    }

    [Theory]
    [InlineData(NotificationKind.Emergency)]
    [InlineData(NotificationKind.Geofence)]
    [InlineData(NotificationKind.Anomaly)]
    public void ForWalk_Alerts_AreHighPriority(NotificationKind kind)
    {
        Walk walk = RequestedWalk();
        walk.Accept("walker-1", s_now);

        Notification.ForWalk(walk, WalkParty.Walker, kind, s_now).Priority.ShouldBe(NotificationPriority.High);
    }

    [Fact]
    public void ForWalk_ToTheWalker_GoesToTheAssignedWalker()
    {
        Walk walk = RequestedWalk();
        walk.Accept("walker-1", s_now);

        Notification.ForWalk(walk, WalkParty.Walker, NotificationKind.WalkCancelled, s_now).UserId.ShouldBe("walker-1");
    }

    [Fact]
    public void ForWalk_ToTheWalkerOfAWalkWithoutWalker_Throws() =>
        Should.Throw<InvalidOperationException>(
            () => Notification.ForWalk(RequestedWalk(), WalkParty.Walker, NotificationKind.WalkCancelled, s_now));

    [Fact]
    public void MarkRead_Twice_KeepsTheFirstTime()
    {
        Walk walk = RequestedWalk();
        Notification notification = Notification.ForWalk(walk, WalkParty.Owner, NotificationKind.WalkFinished, s_now);

        notification.MarkRead(s_now.AddMinutes(1));
        notification.MarkRead(s_now.AddMinutes(5));

        notification.ReadAt.ShouldBe(s_now.AddMinutes(1));
    }
}
