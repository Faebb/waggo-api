using Waggo.Domain.Common;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Services.Pricing;
using Waggo.Domain.UnitTests.TestData;
using Waggo.Domain.ValueObjects.Pricing;

namespace Waggo.Domain.UnitTests.Services.Pricing;

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
        PricingTable table = PricingTableMother.Default();
        WalkDuration duration = WalkDuration.Create(minutes).Data;

        FareBreakdown fare = FareCalculator.Calculate(table, walkType, duration).Data;

        fare.Total.ShouldBe(Money.Of(total, "COP"));
        fare.Commission.ShouldBe(Money.Of(commission, "COP"));
        fare.WalkerPayout.ShouldBe(Money.Of(payout, "COP"));
    }

    [Fact]
    public void Calculate_CommissionPlusPayout_AlwaysEqualsTotal()
    {
        PricingTable table = PricingTableMother.Default();

        for (int minutes = WalkDuration.MinMinutes;
            minutes <= WalkDuration.MaxMinutes;
            minutes += WalkDuration.StepMinutes)
        {
            foreach (WalkType type in Enum.GetValues<WalkType>())
            {
                FareBreakdown fare = FareCalculator.Calculate(table, type, WalkDuration.Create(minutes).Data).Data;
                fare.Commission.Add(fare.WalkerPayout).ShouldBe(fare.Total);
            }
        }
    }

    [Fact]
    public void Calculate_WalkTypeWithoutRate_Fails()
    {
        PricingTable table = PricingTableMother.Default(WalkType.Individual);

        WalkDuration duration = WalkDuration.Create(60).Data;

        WaggoResponse<FareBreakdown> result = FareCalculator.Calculate(table, WalkType.Group, duration);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "Pricing.WalkTypeNotPriced");
    }
}
