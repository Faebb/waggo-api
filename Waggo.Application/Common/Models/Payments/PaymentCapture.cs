namespace Waggo.Application.Common.Models.Payments;

/// <summary>
/// Charge a hold and split it: <paramref name="Commission"/> stays with Waggo and <paramref name="WalkerPayout"/>
/// goes to the walker (RF-017, RF-018).
/// </summary>
public sealed record PaymentCapture(
    string Reference,
    decimal Amount,
    decimal Commission,
    string WalkerId,
    decimal WalkerPayout,
    string Currency);
