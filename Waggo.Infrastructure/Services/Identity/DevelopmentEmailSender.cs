using Microsoft.Extensions.Logging;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Development/Testing only (ADR-016, same pattern as <c>SimulatedPaymentGateway</c>): writes the email to the log
/// instead of sending it, so a developer can read the confirmation token or 2FA code without a mail provider.
/// </summary>
internal sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email to {To} — {Subject}: {Body}",
            to,
            subject,
            body);
        return Task.CompletedTask;
    }
}
