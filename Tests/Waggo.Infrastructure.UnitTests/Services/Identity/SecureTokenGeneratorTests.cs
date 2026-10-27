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
    public void GenerateNumericCode_ManyCalls_AlwaysReturnsExactlySixDigits()
    {
        for (int call = 0; call < 2000; call++)
        {
            string code = _sut.GenerateNumericCode();

            code.Length.ShouldBe(6);
            code.ShouldAllBe(character => char.IsAsciiDigit(character));
        }
    }

    [Fact]
    public void GenerateNumericCode_ManyCalls_IsNotConstant()
    {
        HashSet<string> codes = Enumerable.Range(0, 200).Select(_ => _sut.GenerateNumericCode()).ToHashSet();

        codes.Count.ShouldBeGreaterThan(150);
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
