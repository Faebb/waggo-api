namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Authentication:Microsoft" (ADR-017). <see cref="ClientIds"/> are the application (client) ids of our
/// app registrations in Microsoft Entra (web, Android, iOS): an <c>idToken</c> is accepted only if it was issued for
/// one of them. Public values, not secrets. With none configured every Microsoft token is refused.
/// </summary>
public sealed class MicrosoftAuthOptions
{
    public const string SectionName = "Authentication:Microsoft";

    public string[] ClientIds { get; set; } = [];
}
