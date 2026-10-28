namespace Waggo.Application.Identity;

/// <summary>What a Google or Microsoft <c>idToken</c> proved (ADR-017): who the account is at the provider and whether
/// the provider itself verified the email.</summary>
public sealed record ExternalIdentity(string ProviderUserId, string Email, bool EmailVerified);
