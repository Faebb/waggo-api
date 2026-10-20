using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Domain.Entities.Payments;

/// <summary>
/// The money of one walk (RF-015 – RF-018). The fare frozen in the walk is held when the owner requests it, then
/// captured when the walk ends (commission for Waggo, the rest for the walker) or released if it is cancelled.
/// Only the gateway reference is stored, never card data (RNF-004).
/// </summary>
public sealed class WalkPayment
{
    // Used by EF Core to materialize the entity.
    private WalkPayment()
    {
    }

    public Guid Id { get; private set; }

    public Guid WalkId { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Who got the payout; set on capture.</summary>
    public string? WalkerId { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal Total { get; private set; }

    public decimal Commission { get; private set; }

    public decimal WalkerPayout { get; private set; }

    public PaymentStatus Status { get; private set; }

    /// <summary>Id of the hold in the payment gateway.</summary>
    public string GatewayReference { get; private set; } = string.Empty;

    public DateTimeOffset HeldAt { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    /// <summary>The gateway accepted the hold of the walk's total.</summary>
    public static WalkPayment Hold(Walk walk, string gatewayReference, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(walk);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayReference);

        return new WalkPayment
        {
            Id = Guid.CreateVersion7(now),
            WalkId = walk.Id,
            OwnerId = walk.OwnerId,
            Currency = walk.Currency,
            Total = walk.Total,
            Commission = walk.Commission,
            WalkerPayout = walk.WalkerPayout,
            Status = PaymentStatus.Held,
            GatewayReference = gatewayReference,
            HeldAt = now,
        };
    }

    /// <summary>The walk ended: charge the owner and pay the walker.</summary>
    public WaggoResponse<WalkPayment> Capture(string walkerId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walkerId);
        WaggoResponse<WalkPayment> response = new();
        if (Status != PaymentStatus.Held)
        {
            response.AddError(PaymentErrors.NotHeld);
            return response;
        }

        Status = PaymentStatus.Captured;
        WalkerId = walkerId;
        CapturedAt = now;
        response.Data = this;
        return response;
    }

    /// <summary>The walk was cancelled before starting: the owner is not charged.</summary>
    public WaggoResponse<WalkPayment> Release(DateTimeOffset now)
    {
        WaggoResponse<WalkPayment> response = new();
        if (Status != PaymentStatus.Held)
        {
            response.AddError(PaymentErrors.NotHeld);
            return response;
        }

        Status = PaymentStatus.Released;
        ReleasedAt = now;
        response.Data = this;
        return response;
    }
}
