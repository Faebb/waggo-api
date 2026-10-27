using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Identity;

/// <summary>Sends a security notice after the account changed, without letting a provider failure undo the change.</summary>
internal static class SecurityNotice
{
    /// <summary>
    /// The account is already changed, so a provider failure must not turn that into an error: it becomes an
    /// internal warning that only reaches the log.
    /// </summary>
    public static async Task SendAsync<T>(
        IEmailSender emailSender,
        string to,
        string subject,
        string body,
        WaggoResponse<T> response,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(to, subject, body, cancellationToken);
        }
        catch (ExternalServiceException exception)
        {
            response.AddWarning(
                EmailErrors.SendFailed.Code,
                $"No se pudo enviar el aviso de seguridad: {exception.Message}",
                MessageVisibility.Internal);
        }
    }
}
