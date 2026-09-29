using System.ComponentModel.DataAnnotations;

namespace Waggo.Infrastructure.Options.Pricing;

/// <summary>Price components of one walk type in configuration.</summary>
public sealed class WalkRateOptions
{
    [Range(0.0, double.MaxValue)]
    public decimal BaseFee { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal PerMinute { get; set; }
}
