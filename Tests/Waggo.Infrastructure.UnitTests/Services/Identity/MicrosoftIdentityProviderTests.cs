using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Waggo.Application.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Identity;
using Waggo.Infrastructure.Options.Identity;
using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>
/// ADR-017: the API validates the Microsoft <c>idToken</c> itself. Tokens are signed with a throw-away key and the
/// "Microsoft keys" are injected. Microsoft says never to trust the <c>email</c> claim on its own, so the email only
/// counts as verified for personal accounts or when the tenant proves the domain (<c>xms_edov</c>).
/// </summary>
public class MicrosoftIdentityProviderTests : IDisposable
{
    private const string ClientId = "11111111-2222-3333-4444-555555555555";
    private const string PersonalTenant = "9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string CompanyTenant = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
    private const string UserObjectId = "0f0f0f0f-1111-2222-3333-444444444444";

    private readonly RSA _microsoftKey = RSA.Create(2048);
    private readonly RSA _otherKey = RSA.Create(2048);

    public void Dispose()
    {
        _microsoftKey.Dispose();
        _otherKey.Dispose();
        GC.SuppressFinalize(this);
    }

    private MicrosoftIdentityProvider Sut(params string[] clientIds)
    {
        OpenIdConnectConfiguration configuration = new();
        configuration.SigningKeys.Add(new RsaSecurityKey(_microsoftKey) { KeyId = "microsoft-key-1" });
        MicrosoftAuthOptions options = new() { ClientIds = clientIds.Length == 0 ? [ClientId] : [.. clientIds] };
        return new MicrosoftIdentityProvider(
            Microsoft.Extensions.Options.Options.Create(options),
            new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            TimeProvider.System,
            NullLogger<MicrosoftIdentityProvider>.Instance);
    }

    private string Token(
        string tenant = PersonalTenant,
        string? issuer = null,
        string audience = ClientId,
        RSA? signWith = null,
        TimeSpan? expiresIn = null,
        Dictionary<string, object>? extraClaims = null,
        string[]? without = null)
    {
        DateTime now = DateTime.UtcNow;
        Dictionary<string, object> claims = new()
        {
            ["sub"] = "pairwise-subject-for-this-app",
            ["oid"] = UserObjectId,
            ["tid"] = tenant,
            ["email"] = "ana@mail.com",
        };
        foreach (KeyValuePair<string, object> claim in extraClaims ?? [])
        {
            claims[claim.Key] = claim.Value;
        }

        foreach (string name in without ?? [])
        {
            claims.Remove(name);
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer ?? $"https://login.microsoftonline.com/{tenant}/v2.0",
            Audience = audience,
            IssuedAt = now.AddMinutes(-5),
            NotBefore = now.AddMinutes(-5),
            Expires = now.Add(expiresIn ?? TimeSpan.FromMinutes(30)),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signWith ?? _microsoftKey) { KeyId = "microsoft-key-1" },
                SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static void ShouldBeRejected(WaggoResponse<ExternalIdentity> result)
    {
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.ExternalTokenInvalid");
    }

    [Fact]
    public void Provider_IsMicrosoft() => Sut().Provider.ShouldBe(ExternalProvider.Microsoft);

    [Fact]
    public async Task ValidateAsync_PersonalAccount_IsIdentifiedByTenantAndObjectIdAndItsEmailIsVerified()
    {
        WaggoResponse<ExternalIdentity> result = await Sut().ValidateAsync(Token(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ProviderUserId.ShouldBe($"{PersonalTenant}:{UserObjectId}");
        result.Data.Email.ShouldBe("ana@mail.com");
        result.Data.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CompanyAccountWithAVerifiedDomain_HasAVerifiedEmail()
    {
        string token = Token(tenant: CompanyTenant, extraClaims: new Dictionary<string, object> { ["xms_edov"] = true });

        WaggoResponse<ExternalIdentity> result = await Sut().ValidateAsync(token, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ProviderUserId.ShouldBe($"{CompanyTenant}:{UserObjectId}");
        result.Data.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CompanyAccountWithoutProofOfTheDomain_HasAnUnverifiedEmail()
    {
        // nOAuth: anyone can set any email in a tenant they control, so without xms_edov the email proves nothing.
        WaggoResponse<ExternalIdentity> result =
            await Sut().ValidateAsync(Token(tenant: CompanyTenant), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.EmailVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_CompanyAccountWhoseDomainIsNotVerified_HasAnUnverifiedEmail()
    {
        string token = Token(tenant: CompanyTenant, extraClaims: new Dictionary<string, object> { ["xms_edov"] = false });

        WaggoResponse<ExternalIdentity> result = await Sut().ValidateAsync(token, CancellationToken.None);

        result.Data.EmailVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_AnyOfSeveralClientIds_IsAccepted()
    {
        WaggoResponse<ExternalIdentity> result = await Sut("web-id", "android-id")
            .ValidateAsync(Token(audience: "android-id"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AudienceThatIsNotOurs_Fails()
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(audience: "someone-elses-app"), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_IssuerOfAnotherTenantThanTheTokenSays_Fails()
    {
        // The issuer must carry the same tenant id as the tid claim.
        string token = Token(tenant: CompanyTenant, issuer: $"https://login.microsoftonline.com/{PersonalTenant}/v2.0");

        ShouldBeRejected(await Sut().ValidateAsync(token, CancellationToken.None));
    }

    [Theory]
    [InlineData("https://evil.example.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0")]
    [InlineData("https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v1.0")]
    [InlineData("https://sts.windows.net/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/")]
    public async Task ValidateAsync_IssuerThatIsNotMicrosoftV2_Fails(string issuer)
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(tenant: CompanyTenant, issuer: issuer), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_Fails()
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(expiresIn: TimeSpan.FromHours(-1)), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_SignedWithAnotherKey_Fails()
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(signWith: _otherKey), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_UnsignedToken_Fails()
    {
        string header = Convert.ToBase64String("""{"alg":"none","typ":"JWT"}"""u8.ToArray()).TrimEnd('=');
        string payload = Convert.ToBase64String(
            """{"iss":"https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0","aud":"11111111-2222-3333-4444-555555555555","oid":"x","tid":"9188040d-6c67-4c5b-b112-36a304b66dad","email":"a@b.co","exp":9999999999}"""u8.ToArray())
            .TrimEnd('=');

        ShouldBeRejected(await Sut().ValidateAsync($"{header}.{payload}.", CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    public async Task ValidateAsync_GarbageToken_Fails(string token)
    {
        ShouldBeRejected(await Sut().ValidateAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_NoClientIdConfigured_RejectsEveryToken()
    {
        MicrosoftAuthOptions options = new() { ClientIds = [] };
        OpenIdConnectConfiguration configuration = new();
        configuration.SigningKeys.Add(new RsaSecurityKey(_microsoftKey) { KeyId = "microsoft-key-1" });
        MicrosoftIdentityProvider sut = new(
            Microsoft.Extensions.Options.Options.Create(options),
            new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            TimeProvider.System,
            NullLogger<MicrosoftIdentityProvider>.Instance);

        ShouldBeRejected(await sut.ValidateAsync(Token(), CancellationToken.None));
    }

    [Theory]
    [InlineData("oid")]
    [InlineData("tid")]
    [InlineData("email")]
    public async Task ValidateAsync_TokenWithoutAClaimWeNeed_Fails(string missing)
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(without: [missing]), CancellationToken.None));
    }
}
