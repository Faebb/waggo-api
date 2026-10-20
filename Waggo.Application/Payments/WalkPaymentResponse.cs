using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Payments;

/// <summary>The payment of a walk as its owner and walker see it.</summary>
public sealed record WalkPaymentResponse(
    Guid WalkId,
    string Status,
    string Currency,
    decimal Total,
    decimal Commission,
    decimal WalkerPayout,
    DateTimeOffset HeldAt,
    DateTimeOffset? CapturedAt,
    DateTimeOffset? ReleasedAt)
{
    public static WalkPaymentResponse From(WalkPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        return new WalkPaymentResponse(
            payment.WalkId,
            payment.Status.ToString(),
            payment.Currency,
            payment.Total,
            payment.Commission,
            payment.WalkerPayout,
            payment.HeldAt,
            payment.CapturedAt,
            payment.ReleasedAt);
    }
}
