namespace Waggo.Api.Infrastructure.Settings;

/// <summary>OAuth 2.0 authorization server that issues the JWT access tokens.</summary>
public sealed class JwtSettings
{
    /// <summary>Issuer URL of the authorization server, e.g. https://login.example.com/realms/waggo.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Expected <c>aud</c> of the access tokens.</summary>
    public string Audience { get; set; } = "waggo-api";

    /// <summary>Claim that carries the roles (depends on the provider, e.g. "roles").</summary>
    public string RoleClaimType { get; set; } = "roles";
}
