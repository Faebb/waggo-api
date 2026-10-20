using System.ComponentModel.DataAnnotations;

namespace Waggo.Infrastructure.Options.Payments;

/// <summary>
/// Bound from the "Payments" configuration section. Until the PO chooses Stripe or MercadoPago the only provider is
/// "Simulated", configured only for Development and Testing: any other environment fails on startup.
/// </summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    public const string Simulated = "Simulated";

    [Required(ErrorMessage = "Payments:Provider is not configured. Only 'Simulated' exists (Development/Testing).")]
    [AllowedValues(Simulated)]
    public string? Provider { get; set; }
}
