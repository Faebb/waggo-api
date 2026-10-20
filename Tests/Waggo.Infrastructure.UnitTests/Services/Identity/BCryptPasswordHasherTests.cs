using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>RNF-001: passwords are hashed with bcrypt, never stored or compared in plain text.</summary>
public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_NeverEqualsThePlainTextPassword()
    {
        string hash = _sut.Hash("Clave123!");

        hash.ShouldNotBe("Clave123!");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = _sut.Hash("Clave123!");

        _sut.Verify("Clave123!", hash).ShouldBeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = _sut.Hash("Clave123!");

        _sut.Verify("OtraClave456!", hash).ShouldBeFalse();
    }
}
