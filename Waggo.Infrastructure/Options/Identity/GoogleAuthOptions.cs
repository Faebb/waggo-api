namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Authentication:Google" (ADR-017). <see cref="ClientIds"/> are the OAuth client ids of our apps (web,
/// Android, iOS): an <c>idToken</c> is accepted only if it was issued for one of them. Public values, not secrets.
/// With none configured every Google token is refused.
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string[] ClientIds { get; set; } = [];
}
