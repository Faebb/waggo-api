using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.LoginWithExternalProvider;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.LoginWithExternalProvider;

/// <summary>RF-001 + RF-005, ADR-017: sign up or sign in with a Google (or Microsoft) account.</summary>
public class LoginWithExternalProviderHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IExternalIdentityProvider _google = Substitute.For<IExternalIdentityProvider>();
    private readonly IExternalLoginRepository _externalLogins = Substitute.For<IExternalLoginRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalkerProfileRepository _walkerProfiles = Substitute.For<IWalkerProfileRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginWithExternalProviderHandler _sut;

    public LoginWithExternalProviderHandlerTests()
    {
        _google.Provider.Returns(ExternalProvider.Google);
        _tokenGenerator.GenerateToken().Returns("raw-token");
        _tokenGenerator.Hash("raw-token").Returns("token-hash");
        _passwordHasher.Hash("raw-token").Returns("unusable-hash");
        _tokenService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(("access-token", 900));
        _sut = new LoginWithExternalProviderHandler(
            [_google], _externalLogins, _users, _walkerProfiles, _refreshTokens, _passwordHasher, _tokenGenerator,
            _tokenService, new FixedTimeProvider());
    }

    private static LoginWithExternalProviderCommand GoogleCommand() => new(ExternalProvider.Google, "id-token");

    private void GoogleAccepts(string providerUserId, string email, bool emailVerified = true)
    {
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>()).Returns(new WaggoResponse<ExternalIdentity>
        {
            Data = new ExternalIdentity(providerUserId, email, emailVerified),
        });
    }

    private static User ConfirmedUser(string email)
    {
        User user = User.Register(email, "stored-hash", "code-hash", s_now.AddDays(-1)).Data;
        user.Confirm("code-hash", s_now.AddDays(-1));
        return user;
    }

    [Fact]
    public async Task HandleAsync_NewEmail_CreatesAConfirmedAccountLinksItAndIssuesASession()
    {
        GoogleAccepts("google-sub-1", "Ana@Mail.com");
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.AccessToken.ShouldBe("access-token");
        result.Data.RefreshToken.ShouldBe("raw-token");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "ana@mail.com" && u.EmailConfirmed && u.PasswordHash == "unusable-hash"),
            Arg.Any<CancellationToken>());
        await _externalLogins.Received(1).AddAsync(
            Arg.Is<ExternalLogin>(l => l.Provider == ExternalProvider.Google && l.ProviderUserId == "google-sub-1"),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailWithAnAccount_LinksTheExistingAccountInsteadOfDuplicatingIt()
    {
        User existing = ConfirmedUser("ana@mail.com");
        GoogleAccepts("google-sub-1", "ana@mail.com");
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(existing);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _externalLogins.Received(1).AddAsync(
            Arg.Is<ExternalLogin>(l => l.UserId == existing.Id), Arg.Any<CancellationToken>());
        existing.PasswordHash.ShouldBe("stored-hash");
    }

    [Fact]
    public async Task HandleAsync_AlreadyLinked_EntersTheSameAccountWithoutLinkingAgain()
    {
        User existing = ConfirmedUser("ana@mail.com");
        GoogleAccepts("google-sub-1", "ana@mail.com");
        _externalLogins.GetAsync(ExternalProvider.Google, "google-sub-1", Arg.Any<CancellationToken>())
            .Returns(ExternalLogin.Create(existing.Id, ExternalProvider.Google, "google-sub-1", s_now));
        _users.GetAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        await _externalLogins.DidNotReceive().AddAsync(Arg.Any<ExternalLogin>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _tokenService.Received(1).IssueAccessToken(existing.Id, Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyLinkedAndTheEmailChangedAtTheProvider_StillEntersByTheProviderUserId()
    {
        User existing = ConfirmedUser("ana@mail.com");
        GoogleAccepts("google-sub-1", "another@mail.com");
        _externalLogins.GetAsync(ExternalProvider.Google, "google-sub-1", Arg.Any<CancellationToken>())
            .Returns(ExternalLogin.Create(existing.Id, ExternalProvider.Google, "google-sub-1", s_now));
        _users.GetAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        _tokenService.Received(1).IssueAccessToken(existing.Id, Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task HandleAsync_ExistingAccountNeverConfirmed_ConfirmsItAndRemovesTheOldPassword()
    {
        // Pre-hijacking: someone registered ana@mail.com with their own password; the real Ana now signs in with Google.
        User unconfirmed = User.Register("ana@mail.com", "attacker-hash", "code-hash", s_now.AddDays(-1)).Data;
        GoogleAccepts("google-sub-1", "ana@mail.com");
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(unconfirmed);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        unconfirmed.EmailConfirmed.ShouldBeTrue();
        unconfirmed.PasswordHash.ShouldBe("unusable-hash");
        await _users.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderRejectsTheToken_FailsAndTouchesNothing()
    {
        WaggoResponse<ExternalIdentity> rejected = new();
        rejected.AddError(IdentityErrors.ExternalTokenInvalid);
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>()).Returns(rejected);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.ExternalTokenInvalid");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _externalLogins.DidNotReceive().AddAsync(Arg.Any<ExternalLogin>(), Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailNotVerifiedByTheProvider_FailsWithoutLinkingOrCreating()
    {
        GoogleAccepts("google-sub-1", "ana@mail.com", emailVerified: false);

        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(GoogleCommand(), CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.ExternalEmailNotVerified");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _externalLogins.DidNotReceive().AddAsync(Arg.Any<ExternalLogin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoProviderRegisteredForTheRequest_FailsWithExternalTokenInvalid()
    {
        WaggoResponse<SessionResponse> result = await _sut.HandleAsync(
            new LoginWithExternalProviderCommand(ExternalProvider.Microsoft, "id-token"), CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.ExternalTokenInvalid");
    }
}
