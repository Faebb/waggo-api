using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>bcrypt password hashing (RNF-001). The work factor is bcrypt's own default cost (10).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
}
