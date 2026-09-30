using Waggo.Application.Common.Models.Payments;
using Waggo.Domain.Common;

namespace Waggo.Application.Common.Interfaces.Payments;

/// <summary>
/// The payment provider (Stripe or MercadoPago, still to be chosen). When the provider cannot be reached, the
/// methods throw <c>ExternalServiceException</c>.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Holds the amount; the data is the gateway reference, or the error <c>Payments.Declined</c>.</summary>
    Task<WaggoResponse<string>> HoldAsync(PaymentHold hold, CancellationToken cancellationToken);

    /// <summary>Charges a hold and sends the walker their part.</summary>
    Task CaptureAsync(PaymentCapture capture, CancellationToken cancellationToken);

    /// <summary>Cancels a hold: the owner is not charged.</summary>
    Task ReleaseAsync(string reference, CancellationToken cancellationToken);
}
