namespace Waggo.Domain.Pricing;

/// <summary>Price components for one walk type: fixed base fee plus a per-minute rate.</summary>
public sealed record WalkRate(WalkType WalkType, Money BaseFee, Money PerMinute);
