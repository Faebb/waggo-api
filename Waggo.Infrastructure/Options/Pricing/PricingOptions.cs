using System.ComponentModel.DataAnnotations;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Infrastructure.Options.Pricing;

/// <summary>Bound from the "Pricing" configuration section. Values are provisional (see vault: Flujo TDD).</summary>
public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "COP";

    [Range(0.0, 1.0)]
    public decimal CommissionRate { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal RoundingIncrement { get; set; } = 1m;

    [Required]
#pragma warning disable CA2227 // Setter required by the configuration binder
    public Dictionary<WalkType, WalkRateOptions> Rates { get; set; } = [];
#pragma warning restore CA2227
}
