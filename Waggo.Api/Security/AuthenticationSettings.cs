namespace Waggo.Api.Security;

/// <summary>"Authentication" section of appsettings.</summary>
public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Development only: authenticates every request as <see cref="DevelopmentUser"/> without a token.
    /// The API refuses to start with it enabled in Production.
    /// </summary>
    public bool UseDevelopmentUser { get; set; }

    /// <summary>OAuth 2.0 authorization server (any provider that issues JWT access tokens).</summary>
    public JwtSettings Jwt { get; set; } = new();

    public DevelopmentUserSettings DevelopmentUser { get; set; } = new();
}

public sealed class JwtSettings
{
    /// <summary>Issuer URL of the authorization server, e.g. https://login.example.com/realms/waggo.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Expected <c>aud</c> of the access tokens.</summary>
    public string Audience { get; set; } = "waggo-api";

    /// <summary>Claim that carries the roles (depends on the provider, e.g. "roles").</summary>
    public string RoleClaimType { get; set; } = "roles";
}

public sealed class DevelopmentUserSettings
{
    public string Id { get; set; } = "dev-user";

    public string Name { get; set; } = "Developer";

#pragma warning disable CA1819 // Bound from configuration
    public string[] Roles { get; set; } = [];
#pragma warning restore CA1819
}
