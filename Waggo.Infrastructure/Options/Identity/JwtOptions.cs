using System.ComponentModel.DataAnnotations;

namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Authentication:Jwt" (ADR-016): the API signs its own access tokens, so this is the symmetric key,
/// not an external authorization server's metadata. 32+ random bytes in base64, outside the code (secret / Key
/// Vault / environment), same pattern as <c>Encryption:Key</c>.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    [Required(ErrorMessage = "Authentication:Jwt:SigningKey is not configured (openssl rand -base64 32).")]
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "waggo-api";

    public string Audience { get; set; } = "waggo-api";

    /// <summary>Must match <c>Authentication:Jwt:RoleClaimType</c> read by the validation side (ADR-011).</summary>
    public string RoleClaimType { get; set; } = "roles";

    public int AccessTokenMinutes { get; set; } = 15;
}
