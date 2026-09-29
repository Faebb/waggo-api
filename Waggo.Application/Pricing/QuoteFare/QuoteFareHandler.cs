using Waggo.Application.Abstractions;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing.QuoteFare;

/// <summary>RF-019: the owner sees the calculated price before confirming a walk.</summary>
internal sealed class QuoteFareHandler(IPricingTableProvider pricingTableProvider)
    : IQueryHandler<QuoteFareQuery, FareQuoteResponse>
{
    public async Task<WaggoResponse<FareQuoteResponse>> HandleAsync(
        QuoteFareQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        WaggoResponse<FareQuoteResponse> response = new WaggoResponse<FareQuoteResponse>();

        WaggoResponse<WalkDuration> duration = WalkDuration.Create(query.DurationMinutes);
        response.ConcatStacks(duration);
        if (response.IsFailure)
        {
            return response;
        }

        WaggoResponse<PricingTable> table = await pricingTableProvider.GetCurrentAsync(cancellationToken);
        response.ConcatStacks(table);
        if (response.IsFailure)
        {
            return response;
        }

        WaggoResponse<FareBreakdown> fare = FareCalculator.Calculate(table.Value, query.WalkType, duration.Value);
        response.ConcatStacks(fare);
        if (response.IsFailure)
        {
            return response;
        }

        FareBreakdown quote = fare.Value;
        response.AddInfo(
            PricingMessages.FareQuoted,
            $"Fare quoted: {query.WalkType} {duration.Value.Minutes} min = {quote.Total}",
            MessageVisibility.Internal);

        return response.SetValue(new FareQuoteResponse(
            query.WalkType.ToString(),
            duration.Value.Minutes,
            quote.Total.Currency,
            quote.Total.Amount,
            quote.Commission.Amount,
            quote.WalkerPayout.Amount));
    }
}
