using Waggo.Domain.Common;

namespace Waggo.Domain.Pricing;

/// <summary>
/// Calculates the fare of a walk (RF-019) and the platform commission (RF-018).
/// total = round(baseFee + perMinute * minutes); commission = round(total * rate); payout = total - commission.
/// </summary>
public static class FareCalculator
{
    public static WaggoResponse<FareBreakdown> Calculate(PricingTable table, WalkType walkType, WalkDuration duration)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(duration);

        WaggoResponse<FareBreakdown> response = new();

        WalkRate? rate = table.RateFor(walkType);
        if (rate is null)
        {
            response.AddError(PricingErrors.WalkTypeNotPriced(walkType));
            return response;
        }

        Money total = rate.BaseFee
            .Add(rate.PerMinute.Multiply(duration.Minutes))
            .RoundToNearest(table.RoundingIncrement);

        Money commission = total.Multiply(table.CommissionRate.Value).RoundToNearest(1m);
        Money payout = total.Subtract(commission);

        response.Data = new FareBreakdown(total, commission, payout);
        return response;
    }
}
