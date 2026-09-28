using Waggo.Domain.Common;

namespace Waggo.Domain.Pricing;

/// <summary>Platform commission as a fraction (0.20 = 20 %) — RF-018.</summary>
public sealed record CommissionRate
{
    private CommissionRate(decimal value) => Value = value;

    public decimal Value { get; }

    public static WaggoResponse<CommissionRate> Create(decimal value)
    {
        if (value is < 0m or > 1m)
        {
            return PricingErrors.InvalidCommissionRate;
        }

        return new CommissionRate(value);
    }
}
