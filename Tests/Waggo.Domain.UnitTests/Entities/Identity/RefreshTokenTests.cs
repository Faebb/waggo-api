using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_IsActiveRightAway()
    {
        RefreshToken token = RefreshToken.Create(s_userId, "token-hash", s_now);

        token.IsActive(s_now).ShouldBeTrue();
        token.ExpiresAt.ShouldBe(s_now.Add(RefreshToken.Lifetime));
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        RefreshToken token = RefreshToken.Create(s_userId, "token-hash", s_now);

        token.IsActive(s_now.Add(RefreshToken.Lifetime).AddSeconds(1)).ShouldBeFalse();
    }

    [Fact]
    public void Rotate_ActiveToken_RevokesItAndLinksTheReplacement()
    {
        RefreshToken token = RefreshToken.Create(s_userId, "token-hash", s_now);
        Guid replacementId = Guid.NewGuid();

        WaggoResponse<RefreshToken> result = token.Rotate(replacementId, s_now);

        result.IsValid.ShouldBeTrue();
        token.RevokedAt.ShouldBe(s_now);
        token.ReplacedByTokenId.ShouldBe(replacementId);
        token.IsActive(s_now).ShouldBeFalse();
    }

    [Fact]
    public void Rotate_AlreadyRevokedToken_FailsWithRefreshTokenInvalid()
    {
        RefreshToken token = RefreshToken.Create(s_userId, "token-hash", s_now);
        token.Rotate(Guid.NewGuid(), s_now);

        WaggoResponse<RefreshToken> result = token.Rotate(Guid.NewGuid(), s_now);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
    }

    [Fact]
    public void Rotate_ExpiredToken_FailsWithRefreshTokenInvalid()
    {
        RefreshToken token = RefreshToken.Create(s_userId, "token-hash", s_now);

        WaggoResponse<RefreshToken> result =
            token.Rotate(Guid.NewGuid(), s_now.Add(RefreshToken.Lifetime).AddSeconds(1));

        result.Errors.Single().Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
    }
}
