using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Identity;

/// <summary>Sends the security notice after a password change, shared by the reset and the voluntary change.</summary>
internal static class PasswordChangedNotice
{
    /// <summary>
    /// The password is already changed, so a provider failure must not turn that into an error: it becomes an
    /// internal warning that only reaches the log.
    /// </summary>
    public static async Task SendAsync<T>(
        IEmailSender emailSender,
        string email,
        WaggoResponse<T> response,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(
                email,
                PasswordChangedEmail.Subject,
                PasswordChangedEmail.Body,
                cancellationToken);
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
