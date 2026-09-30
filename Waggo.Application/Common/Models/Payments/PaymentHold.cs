namespace Waggo.Application.Common.Models.Payments;

/// <summary>Hold <paramref name="Amount"/> on the owner's default payment method for a walk.</summary>
public sealed record PaymentHold(Guid WalkId, string OwnerId, decimal Amount, string Currency);
