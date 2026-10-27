namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>
/// Opaque, high-entropy tokens (refresh tokens) and the short numeric code of the email confirmation. Only the hash
/// is stored; the raw value goes to the user once and is never read back.
/// </summary>
public interface ITokenGenerator
{
    string GenerateToken();

    /// <summary>A random 6-digit code (leading zeros kept) for the person to type from the email (RF-001).</summary>
    string GenerateNumericCode();

    string Hash(string token);
}
