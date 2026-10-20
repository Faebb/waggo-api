namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Port for sending transactional email (confirmation links, 2FA codes). See ADR-016.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
