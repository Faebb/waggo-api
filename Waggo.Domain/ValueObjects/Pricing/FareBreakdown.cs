namespace Waggo.Domain.ValueObjects.Pricing;

/// <summary>Result of pricing a walk: what the owner pays and how it splits (RF-018, RF-019).</summary>
public sealed record FareBreakdown(Money Total, Money Commission, Money WalkerPayout);
