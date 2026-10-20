namespace Waggo.Domain.Enums.Payments;

/// <summary>Where the money of a walk is (RF-016): held from the request until the walk ends or is cancelled.</summary>
public enum PaymentStatus
{
    /// <summary>Authorized on the owner's payment method, not charged yet.</summary>
    Held = 1,

    /// <summary>Charged: Waggo keeps the commission and the rest goes to the walker (RF-017, RF-018).</summary>
    Captured = 2,

    /// <summary>The hold was cancelled: the owner is not charged.</summary>
    Released = 3,
}
