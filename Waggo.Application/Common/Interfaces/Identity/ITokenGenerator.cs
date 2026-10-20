namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>
/// Opaque, high-entropy tokens (email confirmation, later 2FA codes and refresh tokens). Only the hash is stored;
/// the raw value goes to the user once and is never read back.
/// </summary>
public interface ITokenGenerator
{
    string GenerateToken();

    string Hash(string token);
}
