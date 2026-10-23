using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Payments;

/// <summary>A payment method as the owner sees it. Never carries more than the last four digits (RNF-004).</summary>
public sealed record PaymentMethodResponse(
    Guid Id,
    string Brand,
    string LastFour,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault)
{
    public static PaymentMethodResponse From(PaymentMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return new PaymentMethodResponse(
            method.Id,
            method.Brand.ToString(),
            method.LastFour,
            method.ExpiryMonth,
            method.ExpiryYear,
            method.IsDefault);
    }
}
