using Microsoft.Extensions.Logging;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Models.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Infrastructure.Services.Payments;

/// <summary>
/// Stand-in for the real provider (Development and Testing only). Every hold is approved except for users whose id
/// contains <c>card-declined</c>, like the test cards of Stripe, so the decline can be tried end to end.
/// </summary>
internal sealed partial class SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger) : IPaymentGateway
{
    public const string DeclinedMarker = "card-declined";

    public Task<WaggoResponse<string>> HoldAsync(PaymentHold hold, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hold);
        WaggoResponse<string> response = new();
        if (hold.OwnerId.Contains(DeclinedMarker, StringComparison.Ordinal))
        {
            response.AddError(PaymentErrors.Declined);
            return Task.FromResult(response);
        }

        response.Data = $"sim_{hold.WalkId:N}";
        LogHeld(hold.Amount, hold.Currency, response.Data);
        return Task.FromResult(response);
    }

    public Task CaptureAsync(PaymentCapture capture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capture);
        LogCaptured(capture.Reference, capture.Commission, capture.WalkerPayout, capture.WalkerId);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string reference, CancellationToken cancellationToken)
    {
        LogReleased(reference);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulated hold of {Amount} {Currency}: {Reference}")]
    private partial void LogHeld(decimal amount, string currency, string reference);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Simulated capture {Reference}: commission {Commission}, payout {Payout} to {WalkerId}")]
    private partial void LogCaptured(string reference, decimal commission, decimal payout, string walkerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulated release {Reference}")]
    private partial void LogReleased(string reference);
}
