namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Port for password hashing (RNF-001). The plain text password never reaches storage.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
