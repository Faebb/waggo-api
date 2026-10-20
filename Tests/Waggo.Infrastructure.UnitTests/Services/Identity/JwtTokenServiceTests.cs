using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Waggo.Infrastructure.Options.Identity;
using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>RNF-002: the API signs and reads its own access tokens (ADR-016).</summary>
public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        Issuer = "waggo-api",
        Audience = "waggo-api",
        RoleClaimType = "roles",
        AccessTokenMinutes = 15,
    };

    private JwtTokenService Sut() => new(Microsoft.Extensions.Options.Options.Create(_options));

    [Fact]
    public void IssueAccessToken_ReturnsAReadableTokenWithTheSubjectAndRoles()
    {
        Guid userId = Guid.NewGuid();

        (string accessToken, int expiresInSeconds) = Sut().IssueAccessToken(userId, ["owner", "walker"]);

        expiresInSeconds.ShouldBe(15 * 60);
        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        token.Issuer.ShouldBe("waggo-api");
        token.Claims.Single(c => c.Type == "sub").Value.ShouldBe(userId.ToString());
        token.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ShouldBe(["owner", "walker"]);
    }

    [Fact]
    public void IssueAccessToken_ExpiresAfterTheConfiguredMinutes()
    {
        JwtSecurityToken token = new JwtSecurityTokenHandler()
            .ReadJwtToken(Sut().IssueAccessToken(Guid.NewGuid(), ["owner"]).AccessToken);

        (token.ValidTo - token.ValidFrom).ShouldBe(TimeSpan.FromMinutes(15));
    }
}
