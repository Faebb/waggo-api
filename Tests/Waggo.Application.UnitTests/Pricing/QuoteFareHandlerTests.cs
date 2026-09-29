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
        WaggoResponse<PricingTable> table = new()
        {
            Data = new PricingTable(
                "COP",
                [new WalkRate(WalkType.Individual, Money.Of(8000m, "COP"), Money.Of(250m, "COP"))],
                CommissionRate.Create(0.20m).Data,
                roundingIncrement: 100m),
        };
        _provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(table);

        _sut = new QuoteFareHandler(new QuoteFareQueryValidator(), _provider);
    }

    private Task<WaggoResponse<FareQuoteResponse>> QuoteAsync(WalkType walkType, int minutes) =>
        _sut.HandleAsync(new QuoteFareQuery(walkType, minutes), CancellationToken.None);

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsQuote()
    {
        WaggoResponse<FareQuoteResponse> result = await QuoteAsync(WalkType.Individual, 60);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBe(new FareQuoteResponse("Individual", 60, "COP", 23000m, 4600m, 18400m));
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_AddsInternalInfoForTheLog()
    {
        WaggoResponse<FareQuoteResponse> result = await QuoteAsync(WalkType.Individual, 60);

        WaggoMessage info = result.Infos.Single(i => i.Code == PricingMessages.FareQuoted);
        info.Visibility.ShouldBe(MessageVisibility.Internal);
    }

    [Fact]
    public async Task HandleAsync_InvalidDuration_FailsWithoutLoadingPricing()
    {
        WaggoResponse<FareQuoteResponse> result = await QuoteAsync(WalkType.Individual, 20);

        result.Errors.ShouldContain(e => e.Code == PricingErrors.InvalidDuration.Code);
        await _provider.DidNotReceive().GetCurrentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalkTypeWithoutRate_Fails()
    {
        WaggoResponse<FareQuoteResponse> result = await QuoteAsync(WalkType.Group, 60);

        result.Errors.ShouldContain(e => e.Code == "Pricing.WalkTypeNotPriced");
    }

    [Fact]
    public async Task HandleAsync_ProviderFailsOrWarns_ConcatenatesItsStacks()
    {
        WaggoResponse<PricingTable> failing = new();
        failing.AddWarning("Pricing.StaleRates", "Rates are older than 24h");
        failing.AddError("Pricing.RatesUnavailable", "No rates", ErrorType.Unexpected);
        _provider.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(failing);

        WaggoResponse<FareQuoteResponse> result = await QuoteAsync(WalkType.Individual, 60);

        result.Warnings.ShouldContain(w => w.Code == "Pricing.StaleRates");
        result.Errors.ShouldContain(e => e.Code == "Pricing.RatesUnavailable");
        result.ErrorType.ShouldBe(ErrorType.Unexpected);
    }
}
