namespace Waggo.Api.Infrastructure.Settings;

/// <summary>
/// ADR-016: the API is its own authorization server, so this validates against its own symmetric signing key
/// instead of an external provider's OIDC metadata (<c>Authority</c>, superseded).
/// </summary>
public sealed class JwtSettings
{
    /// <summary>32+ random bytes in base64 (<c>openssl rand -base64 32</c>); same value as <c>Waggo.Infrastructure.Options.Identity.JwtOptions.SigningKey</c>.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "waggo-api";

    /// <summary>Expected <c>aud</c> of the access tokens.</summary>
    public string Audience { get; set; } = "waggo-api";

    /// <summary>Claim that carries the roles.</summary>
    public string RoleClaimType { get; set; } = "roles";
}
