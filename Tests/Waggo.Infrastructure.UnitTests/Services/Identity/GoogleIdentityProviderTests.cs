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
/// ADR-017: the API validates the Google <c>idToken</c> itself (signature, issuer, audience, lifetime). Tokens are
/// signed with a throw-away key and the "Google keys" are injected, so no test talks to Google.
/// </summary>
public class GoogleIdentityProviderTests : IDisposable
{
    private const string ClientId = "waggo-web.apps.googleusercontent.com";

    private readonly RSA _googleKey = RSA.Create(2048);
    private readonly RSA _otherKey = RSA.Create(2048);

    public void Dispose()
    {
        _googleKey.Dispose();
        _otherKey.Dispose();
        GC.SuppressFinalize(this);
    }

    private GoogleIdentityProvider Sut(params string[] clientIds)
    {
        OpenIdConnectConfiguration configuration = new();
        configuration.SigningKeys.Add(new RsaSecurityKey(_googleKey) { KeyId = "google-key-1" });
        GoogleAuthOptions options = new() { ClientIds = clientIds.Length == 0 ? [ClientId] : [.. clientIds] };
        return new GoogleIdentityProvider(
            Microsoft.Extensions.Options.Options.Create(options),
            new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            TimeProvider.System,
            NullLogger<GoogleIdentityProvider>.Instance);
    }

    private string Token(
        string issuer = "https://accounts.google.com",
        string audience = ClientId,
        RSA? signWith = null,
        TimeSpan? expiresIn = null,
        Dictionary<string, object>? claims = null)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan lifetime = expiresIn ?? TimeSpan.FromMinutes(30);
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now.AddMinutes(-5),
            NotBefore = now.AddMinutes(-5),
            Expires = now.Add(lifetime),
            Claims = claims ?? new Dictionary<string, object>
            {
                ["sub"] = "google-sub-1",
                ["email"] = "ana@mail.com",
                ["email_verified"] = true,
            },
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signWith ?? _googleKey) { KeyId = "google-key-1" }, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static void ShouldBeRejected(WaggoResponse<ExternalIdentity> result)
    {
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.ExternalTokenInvalid");
    }

    [Fact]
    public void Provider_IsGoogle() => Sut().Provider.ShouldBe(ExternalProvider.Google);

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsTheSubjectEmailAndItsVerification()
    {
        WaggoResponse<ExternalIdentity> result = await Sut().ValidateAsync(Token(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ProviderUserId.ShouldBe("google-sub-1");
        result.Data.Email.ShouldBe("ana@mail.com");
        result.Data.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_IssuerWithoutScheme_IsAlsoAccepted()
    {
        WaggoResponse<ExternalIdentity> result =
            await Sut().ValidateAsync(Token(issuer: "accounts.google.com"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EmailNotVerified_ReturnsItSoTheCallerDecides()
    {
        string token = Token(claims: new Dictionary<string, object>
        {
            ["sub"] = "google-sub-1",
            ["email"] = "ana@mail.com",
            ["email_verified"] = false,
        });

        WaggoResponse<ExternalIdentity> result = await Sut().ValidateAsync(token, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
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
    public async Task ValidateAsync_WrongIssuer_Fails()
    {
        ShouldBeRejected(await Sut().ValidateAsync(Token(issuer: "https://evil.example.com"), CancellationToken.None));
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
            """{"iss":"https://accounts.google.com","aud":"waggo-web.apps.googleusercontent.com","sub":"x","email":"a@b.co","exp":9999999999}"""u8.ToArray())
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
        GoogleAuthOptions options = new() { ClientIds = [] };
        OpenIdConnectConfiguration configuration = new();
        configuration.SigningKeys.Add(new RsaSecurityKey(_googleKey) { KeyId = "google-key-1" });
        GoogleIdentityProvider sut = new(
            Microsoft.Extensions.Options.Options.Create(options),
            new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration),
            TimeProvider.System,
            NullLogger<GoogleIdentityProvider>.Instance);

        ShouldBeRejected(await sut.ValidateAsync(Token(), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_TokenWithoutEmail_Fails()
    {
        string token = Token(claims: new Dictionary<string, object> { ["sub"] = "google-sub-1" });

        ShouldBeRejected(await Sut().ValidateAsync(token, CancellationToken.None));
    }
}
