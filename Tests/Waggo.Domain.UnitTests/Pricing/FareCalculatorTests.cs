using Waggo.Domain.Pricing;

namespace Waggo.Domain.UnitTests.Pricing;

/// <summary>RF-019 (fare) and RF-018 (commission). Cases come from the examples table agreed with the PO.</summary>
public class FareCalculatorTests
{
    public static TheoryData<WalkType, int, decimal, decimal, decimal> Examples => new()
    {
        { WalkType.Individual, 60, 23000m, 4600m, 18400m },
        { WalkType.Individual, 30, 15500m, 3100m, 12400m },
        { WalkType.Group, 45, 11800m, 2360m, 9440m }, // 11 750 rounded to nearest 100
    };

    [Theory]
    [MemberData(nameof(Examples))]
    public void Calculate_ReturnsTotalCommissionAndPayout(
        WalkType walkType, int minutes, decimal total, decimal commission, decimal payout)
    {
        var table = PricingTableMother.Default();
        var duration = WalkDuration.Create(minutes).Value;

        var fare = FareCalculator.Calculate(table, walkType, duration).Value;

        fare.Total.ShouldBe(Money.Of(total, "COP"));
        fare.Commission.ShouldBe(Money.Of(commission, "COP"));
        fare.WalkerPayout.ShouldBe(Money.Of(payout, "COP"));
    }

    [Fact]
    public void Calculate_CommissionPlusPayout_AlwaysEqualsTotal()
    {
        var table = PricingTableMother.Default();

        for (var minutes = WalkDuration.MinMinutes; minutes <= WalkDuration.MaxMinutes; minutes += WalkDuration.StepMinutes)
        {
            foreach (var type in Enum.GetValues<WalkType>())
            {
                var fare = FareCalculator.Calculate(table, type, WalkDuration.Create(minutes).Value).Value;
                fare.Commission.Add(fare.WalkerPayout).ShouldBe(fare.Total);
            }
        }
    }

    [Fact]
    public void Calculate_WalkTypeWithoutRate_Fails()
    {
        var table = PricingTableMother.Default(WalkType.Individual);

        var result = FareCalculator.Calculate(table, WalkType.Group, WalkDuration.Create(60).Value);

        result.IsFailure.ShouldBeTrue();
        result.HasError("Pricing.WalkTypeNotPriced").ShouldBeTrue();
    }
}
