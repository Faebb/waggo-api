using Waggo.Domain.ValueObjects.Pricing;

namespace Waggo.Domain.UnitTests.ValueObjects.Pricing;

public class MoneyTests
{
    [Fact]
    public void Of_NegativeAmount_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Money.Of(-1m, "COP"));

    [Theory]
    [InlineData("")]
    [InlineData("CO")]
    [InlineData("COPS")]
    public void Of_InvalidCurrency_Throws(string currency) =>
        Should.Throw<ArgumentException>(() => Money.Of(10m, currency));

    [Fact]
    public void Of_NormalizesCurrencyToUpperCase() =>
        Money.Of(10m, "cop").Currency.ShouldBe("COP");

    [Fact]
    public void Add_SameCurrency_SumsAmounts() =>
        Money.Of(1000m, "COP").Add(Money.Of(500m, "COP")).ShouldBe(Money.Of(1500m, "COP"));

    [Fact]
    public void Add_DifferentCurrency_Throws() =>
        Should.Throw<InvalidOperationException>(() => Money.Of(1m, "COP").Add(Money.Of(1m, "USD")));

    [Fact]
    public void Subtract_ResultBelowZero_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Money.Of(1m, "COP").Subtract(Money.Of(2m, "COP")));

    public static TheoryData<decimal, decimal, decimal> RoundingCases => new()
    {
        { 11750m, 100m, 11800m },
        { 11749m, 100m, 11700m },
        { 23000m, 100m, 23000m },
        { 2360.4m, 1m, 2360m },
    };

    [Theory]
    [MemberData(nameof(RoundingCases))]
    public void RoundToNearest_RoundsHalfAwayFromZero(decimal amount, decimal increment, decimal expected) =>
        Money.Of(amount, "COP").RoundToNearest(increment).Amount.ShouldBe(expected);

    [Fact]
    public void Equality_IsByValue() =>
        Money.Of(100m, "COP").ShouldBe(Money.Of(100m, "COP"));
}
