using Waggo.Application.Abstractions;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing.QuoteFare;

/// <summary>RF-019: the owner sees the calculated price before confirming a walk.</summary>
internal sealed class QuoteFareHandler(IPricingTableProvider pricingTableProvider)
    : IQueryHandler<QuoteFareQuery, FareQuoteResponse>
{
    public async Task<Result<FareQuoteResponse>> HandleAsync(QuoteFareQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var duration = WalkDuration.Create(query.DurationMinutes);
        if (duration.IsFailure)
        {
            return duration.Error;
        }

        var table = await pricingTableProvider.GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        return FareCalculator
            .Calculate(table, query.WalkType, duration.Value)
            .Map(fare => new FareQuoteResponse(
                query.WalkType.ToString(),
                duration.Value.Minutes,
                fare.Total.Currency,
                fare.Total.Amount,
                fare.Commission.Amount,
                fare.WalkerPayout.Amount));
    }
}
