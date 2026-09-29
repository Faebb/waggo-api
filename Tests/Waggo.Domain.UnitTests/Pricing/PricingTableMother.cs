using Waggo.Domain.Pricing;

namespace Waggo.Domain.UnitTests.Pricing;

/// <summary>
/// Object Mother with the provisional rates documented in the vault (Flujo TDD › Ejemplo real RF-019).
/// </summary>
internal static class PricingTableMother
{
    public static PricingTable Default(params WalkType[] only)
    {
        WalkRate[] rates =
        {
            new WalkRate(WalkType.Individual, Money.Of(8000m, "COP"), Money.Of(250m, "COP")),
            new WalkRate(WalkType.Group, Money.Of(5000m, "COP"), Money.Of(150m, "COP")),
        };

        return new PricingTable(
            "COP",
            only.Length == 0 ? rates : rates.Where(r => only.Contains(r.WalkType)),
            CommissionRate.Create(0.20m).Data,
            roundingIncrement: 100m);
    }
}
