using Waggo.Domain.Enums.Pricing;

namespace Waggo.Domain.ValueObjects.Pricing;

/// <summary>Current pricing configuration: one rate per walk type, commission and rounding rule.</summary>
public sealed class PricingTable
{
    private readonly Dictionary<WalkType, WalkRate> _rates;

    public PricingTable(
        string currency,
        IEnumerable<WalkRate> rates,
        CommissionRate commissionRate,
        decimal roundingIncrement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(commissionRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roundingIncrement);

        Currency = currency;
        CommissionRate = commissionRate;
        RoundingIncrement = roundingIncrement;
        _rates = rates.ToDictionary(r => r.WalkType);
    }

    public string Currency { get; }

    public CommissionRate CommissionRate { get; }

    public decimal RoundingIncrement { get; }

    public WalkRate? RateFor(WalkType walkType) => _rates.GetValueOrDefault(walkType);
}
