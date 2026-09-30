using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Errors.Payments;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Entities.Payments;

public class WalkPaymentTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static readonly Walk s_walk = Walk.Request(
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

    private static WalkPayment Held() => WalkPayment.Hold(s_walk, "sim_hold_1", s_now);

    [Fact]
    public void Hold_Walk_KeepsTheFrozenFareAndTheGatewayReference()
    {
        WalkPayment payment = Held();

        payment.Status.ShouldBe(PaymentStatus.Held);
        (payment.WalkId, payment.OwnerId, payment.GatewayReference).ShouldBe((s_walk.Id, "owner-1", "sim_hold_1"));
        (payment.Total, payment.Commission, payment.WalkerPayout, payment.Currency)
            .ShouldBe((23000m, 4600m, 18400m, "COP"));
        payment.HeldAt.ShouldBe(s_now);
    }

    [Fact]
    public void Capture_Held_PaysTheWalker()
    {
        WalkPayment payment = Held();

        WaggoResponse<WalkPayment> result = payment.Capture("walker-1", s_now.AddHours(1));

        result.IsValid.ShouldBeTrue();
        (payment.Status, payment.WalkerId, payment.CapturedAt)
            .ShouldBe((PaymentStatus.Captured, "walker-1", s_now.AddHours(1)));
    }

    [Fact]
    public void Release_Held_ReturnsTheMoney()
    {
        WalkPayment payment = Held();

        payment.Release(s_now.AddMinutes(5)).IsValid.ShouldBeTrue();

        (payment.Status, payment.ReleasedAt).ShouldBe((PaymentStatus.Released, s_now.AddMinutes(5)));
    }

    [Fact]
    public void Capture_Released_FailsWithNotHeld()
    {
        WalkPayment payment = Held();
        payment.Release(s_now);

        payment.Capture("walker-1", s_now).Errors.Single().Code.ShouldBe(PaymentErrors.NotHeld.Code);
        payment.Status.ShouldBe(PaymentStatus.Released);
    }

    [Fact]
    public void Release_Captured_FailsWithNotHeld()
    {
        WalkPayment payment = Held();
        payment.Capture("walker-1", s_now);

        payment.Release(s_now).Errors.Single().Code.ShouldBe(PaymentErrors.NotHeld.Code);
        payment.Status.ShouldBe(PaymentStatus.Captured);
    }
}
