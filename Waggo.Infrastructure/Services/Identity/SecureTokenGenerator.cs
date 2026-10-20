using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Opaque tokens for confirmation links and (later) refresh tokens: 32 random bytes, hashed with SHA-256 before
/// storage. A fast hash is correct here (unlike passwords) because the input already has enough entropy to make
/// brute force infeasible.
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
