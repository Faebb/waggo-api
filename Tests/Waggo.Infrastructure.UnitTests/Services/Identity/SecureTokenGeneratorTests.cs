using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>RF-001 confirmation tokens and refresh tokens (RNF-002) share this generator.</summary>
public class SecureTokenGeneratorTests
{
    private readonly SecureTokenGenerator _sut = new();

    [Fact]
    public void GenerateToken_TwoCalls_ProduceDifferentValues()
    {
        _sut.GenerateToken().ShouldNotBe(_sut.GenerateToken());
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
