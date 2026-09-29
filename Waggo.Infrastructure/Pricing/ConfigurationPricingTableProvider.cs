using Microsoft.Extensions.Options;
using Waggo.Application.Pricing;
using Waggo.Domain.Common;
using Waggo.Domain.Pricing;

namespace Waggo.Infrastructure.Pricing;

/// <summary>
/// Adapter: builds the pricing table from configuration. Replace with a DB-backed provider when admins manage rates.
/// </summary>
internal sealed class ConfigurationPricingTableProvider(IOptionsMonitor<PricingOptions> options) : IPricingTableProvider
{
    public Task<WaggoResponse<PricingTable>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        PricingOptions o = options.CurrentValue;
        WaggoResponse<PricingTable> response = new WaggoResponse<PricingTable>();

        WaggoResponse<CommissionRate> commission = CommissionRate.Create(o.CommissionRate);
        response.ConcatStacks(commission);
        if (!response.IsValid)
        {
            return Task.FromResult(response);
        }

        IEnumerable<WalkRate> rates = o.Rates.Select(kv => new WalkRate(
            kv.Key,
            Money.Of(kv.Value.BaseFee, o.Currency),
            Money.Of(kv.Value.PerMinute, o.Currency)));

        return Task.FromResult(response.SetValue(
            new PricingTable(o.Currency, rates, commission.Value, o.RoundingIncrement)));
    }
}
