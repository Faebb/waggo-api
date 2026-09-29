using Waggo.Domain.Errors.Pricing;
using Waggo.Domain.ValueObjects.Pricing;

namespace Waggo.Domain.UnitTests.ValueObjects.Pricing;

public class WalkDurationTests
{
    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(120)]
    public void Create_ValidMinutes_Succeeds(int minutes) =>
        WalkDuration.Create(minutes).Data.Minutes.ShouldBe(minutes);

    [Theory]
    [InlineData(0)]
    [InlineData(20)]   // below minimum
    [InlineData(50)]   // not a multiple of 15
    [InlineData(135)]  // above maximum
    public void Create_InvalidMinutes_FailsWithInvalidDuration(int minutes) =>
        WalkDuration.Create(minutes).Errors.ShouldContain(e => e.Code == PricingErrors.InvalidDuration.Code);
}
