using FluentValidation;
using Waggo.Application.Abstractions;
using Waggo.Application.Common.Validation;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing.QuoteFare;

/// <summary>RF-019: the owner sees the calculated price before confirming a walk.</summary>
internal sealed class QuoteFareHandler(
    IValidator<QuoteFareQuery> validator,
    IPricingTableProvider pricingTableProvider)
    : IQueryHandler<QuoteFareQuery, FareQuoteResponse>
{
    public async Task<WaggoResponse<FareQuoteResponse>> HandleAsync(
        QuoteFareQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        WaggoResponse<FareQuoteResponse> response = new();

        WaggoResponse<QuoteFareQuery> validation = await validator.ValidateToResponseAsync(query, cancellationToken);
        response.ConcatStacks(validation);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<WalkDuration> duration = WalkDuration.Create(query.DurationMinutes);
        response.ConcatStacks(duration);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<PricingTable> table = await pricingTableProvider.GetCurrentAsync(cancellationToken);
        response.ConcatStacks(table);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<FareBreakdown> fare = FareCalculator.Calculate(table.Data, query.WalkType, duration.Data);
        response.ConcatStacks(fare);
        if (!response.IsValid)
        {
            return response;
        }

        response.AddInfo(
            PricingMessages.FareQuoted,
            $"Fare quoted: {query.WalkType} {duration.Data.Minutes} min = {fare.Data.Total}",
            MessageVisibility.Internal);

        response.Data = new FareQuoteResponse(
            query.WalkType.ToString(),
            duration.Data.Minutes,
            fare.Data.Total.Currency,
            fare.Data.Total.Amount,
            fare.Data.Commission.Amount,
            fare.Data.WalkerPayout.Amount);

        return response;
    }
}
