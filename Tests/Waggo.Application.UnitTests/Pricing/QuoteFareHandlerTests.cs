using Waggo.Application.Pricing;
using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Application.UnitTests.Pricing;

public class QuoteFareHandlerTests
{
    private readonly IPricingTableProvider _provider = Substitute.For<IPricingTableProvider>();
    private readonly QuoteFareHandler _sut;

    public QuoteFareHandlerTests()
    {
        _provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(WaggoResponse.Success(new PricingTable(
            "COP",
            [new WalkRate(WalkType.Individual, Money.Of(8000m, "COP"), Money.Of(250m, "COP"))],
            CommissionRate.Create(0.20m).Value,
            roundingIncrement: 100m)));

        _sut = new QuoteFareHandler(_provider);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsQuote()
    {
        var result = await _sut.HandleAsync(new QuoteFareQuery(WalkType.Individual, 60), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new FareQuoteResponse("Individual", 60, "COP", 23000m, 4600m, 18400m));
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_AddsInternalInfoForTheLog()
    {
        var result = await _sut.HandleAsync(new QuoteFareQuery(WalkType.Individual, 60), CancellationToken.None);

        var info = result.Infos.Single(i => i.Code == PricingMessages.FareQuoted);
        info.Visibility.ShouldBe(MessageVisibility.Internal);
    }

    [Fact]
    public async Task HandleAsync_InvalidDuration_FailsWithoutLoadingPricing()
    {
        var result = await _sut.HandleAsync(new QuoteFareQuery(WalkType.Individual, 20), CancellationToken.None);

        result.HasError(PricingErrors.InvalidDuration.Code).ShouldBeTrue();
        await _provider.DidNotReceive().GetCurrentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalkTypeWithoutRate_Fails()
    {
        var result = await _sut.HandleAsync(new QuoteFareQuery(WalkType.Group, 60), CancellationToken.None);

        result.HasError("Pricing.WalkTypeNotPriced").ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ProviderFailsOrWarns_ConcatenatesItsStacks()
    {
        _provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(
            new WaggoResponse<PricingTable>()
                .AddWarning("Pricing.StaleRates", "Rates are older than 24h")
                .AddError("Pricing.RatesUnavailable", "No rates", ErrorType.Unexpected));

        var result = await _sut.HandleAsync(new QuoteFareQuery(WalkType.Individual, 60), CancellationToken.None);

        result.HasWarning("Pricing.StaleRates").ShouldBeTrue();
        result.HasError("Pricing.RatesUnavailable").ShouldBeTrue();
        result.ErrorType.ShouldBe(ErrorType.Unexpected);
    }
}
