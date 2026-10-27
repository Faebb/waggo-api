namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Email" (RF-001, ADR-016). Without a provider the email is only written to the log, which Production
/// refuses (see <c>EmailStartupGuard</c>). AWS credentials are never configured here: the SDK takes them from the
/// standard chain (AWS_PROFILE, environment variables or the IAM role of the service).
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public EmailProvider Provider { get; set; } = EmailProvider.Development;

    /// <summary>Verified sender, for example <c>no-reply@yourdomain</c>.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Display name shown next to the sender. Empty means only the address.</summary>
    public string FromName { get; set; } = "Waggo";

    public SesOptions Ses { get; set; } = new();
}
