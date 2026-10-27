using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;

namespace Waggo.Application.Identity;

/// <summary>The security notice after a password change, shared by the reset and the voluntary change.</summary>
internal static class PasswordChangedNotice
{
    public static Task SendAsync<T>(
        IEmailSender emailSender,
        string email,
        WaggoResponse<T> response,
        CancellationToken cancellationToken) =>
        SecurityNotice.SendAsync(
            emailSender,
            email,
            PasswordChangedEmail.Subject,
            PasswordChangedEmail.Body,
            response,
            cancellationToken);
}
