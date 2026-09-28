using Waggo.Domain.Pricing;

namespace Waggo.Domain.UnitTests.Pricing;

public class CommissionRateTests
{
    public static TheoryData<decimal> ValidRates => new() { 0m, 0.2m, 1m };

    public static TheoryData<decimal> InvalidRates => new() { -0.01m, 1.01m };

    [Theory]
    [MemberData(nameof(ValidRates))]
    public void Create_BetweenZeroAndOne_Succeeds(decimal value) =>
        CommissionRate.Create(value).Value.Value.ShouldBe(value);

    [Theory]
    [MemberData(nameof(InvalidRates))]
    public void Create_OutOfRange_Fails(decimal value) =>
        CommissionRate.Create(value).Error.ShouldBe(PricingErrors.InvalidCommissionRate);
}
