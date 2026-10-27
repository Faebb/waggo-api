using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Opaque refresh tokens (32 random bytes) and the 6-digit confirmation code, hashed with SHA-256 before storage.
/// A fast hash is correct here (unlike passwords) for the tokens, which have enough entropy to make brute force
/// infeasible. The 6-digit code does not: its defense is the short life and the limit of attempts (<c>User</c>).
/// </summary>
public sealed class SecureTokenGenerator : ITokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string GenerateToken()
    {
        byte[] bytes = new byte[TokenSizeBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public string GenerateNumericCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
