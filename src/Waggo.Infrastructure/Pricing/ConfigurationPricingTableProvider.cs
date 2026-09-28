using Microsoft.Extensions.Options;
using Waggo.Application.Pricing;
using Waggo.Domain.Pricing;

namespace Waggo.Infrastructure.Pricing;

/// <summary>Adapter: builds the pricing table from configuration. Replace with a DB-backed provider when admins manage rates.</summary>
internal sealed class ConfigurationPricingTableProvider(IOptionsMonitor<PricingOptions> options) : IPricingTableProvider
{
    public Task<PricingTable> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var o = options.CurrentValue;

        var commission = CommissionRate.Create(o.CommissionRate);
        if (commission.IsFailure)
        {
            throw new InvalidOperationException(commission.Error.Message);
        }

        var rates = o.Rates.Select(kv => new WalkRate(
            kv.Key,
            Money.Of(kv.Value.BaseFee, o.Currency),
            Money.Of(kv.Value.PerMinute, o.Currency)));

        return Task.FromResult(new PricingTable(o.Currency, rates, commission.Value, o.RoundingIncrement));
    }
}
