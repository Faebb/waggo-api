namespace Waggo.Api.Infrastructure.Settings;

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
