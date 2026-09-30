using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.UnitTests.TestData;

/// <summary>Object Mother for walks and the provisional pricing table of RF-019.</summary>
internal static class WalkMother
{
    public static PricingTable Pricing() =>
        new(
            "COP",
            [
                new WalkRate(WalkType.Individual, Money.Of(8000m, "COP"), Money.Of(250m, "COP")),
                new WalkRate(WalkType.Group, Money.Of(5000m, "COP"), Money.Of(150m, "COP")),
            ],
            CommissionRate.Create(0.20m).Data,
            roundingIncrement: 100m);

    public static WaggoResponse<PricingTable> PricingResponse() => new() { Data = Pricing() };

    /// <summary>A requested walk of <paramref name="ownerId"/> for one dog, individual, 60 minutes.</summary>
    public static Walk Requested(string ownerId = "owner-1", Guid? petId = null) =>
        Walk.Request(
            ownerId,
            [petId ?? Guid.NewGuid()],
            WalkType.Individual,
            WalkDuration.Create(60).Data,
            "Cra 7 # 45-10, Bogotá",
            GeoPoint.Create(4.6361, -74.0645).Data,
            null,
            null,
            new FareBreakdown(Money.Of(23000m, "COP"), Money.Of(4600m, "COP"), Money.Of(18400m, "COP")),
            FixedTimeProvider.Default).Data;
}
