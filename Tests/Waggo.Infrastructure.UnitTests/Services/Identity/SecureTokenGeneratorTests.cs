using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>RF-001 (confirmation token) and RF-005 (2FA code) share this generator.</summary>
public class SecureTokenGeneratorTests
{
    private readonly SecureTokenGenerator _sut = new();

    [Fact]
    public void GenerateToken_TwoCalls_ProduceDifferentValues()
    {
        _sut.GenerateToken().ShouldNotBe(_sut.GenerateToken());
    }

    [Fact]
    public void GenerateNumericCode_IsAlwaysSixDigits()
    {
        for (int i = 0; i < 50; i++)
        {
            string code = _sut.GenerateNumericCode();
            code.Length.ShouldBe(6);
            code.ShouldMatch("^[0-9]{6}$");
        }
    }

    [Fact]
    public void Hash_SameInput_IsDeterministic()
    {
        string token = _sut.GenerateToken();

        _sut.Hash(token).ShouldBe(_sut.Hash(token));
    }

    [Fact]
    public void Hash_NeverEqualsTheRawToken()
    {
        string token = _sut.GenerateToken();

        _sut.Hash(token).ShouldNotBe(token);
    }
}
