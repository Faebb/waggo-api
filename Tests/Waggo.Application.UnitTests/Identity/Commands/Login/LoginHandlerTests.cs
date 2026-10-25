using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.Login;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.Login;

public class LoginHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalkerProfileRepository _walkerProfiles = Substitute.For<IWalkerProfileRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _tokenGenerator.GenerateToken().Returns("raw-refresh-token");
        _tokenGenerator.Hash("raw-refresh-token").Returns("refresh-hash");
        _tokenService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(("access-token", 900));
        _sut = new LoginHandler(
            _users, _walkerProfiles, _refreshTokens, _passwordHasher, _tokenGenerator, _tokenService,
            new FixedTimeProvider());
    }

    private static User ConfirmedUser()
    {
        User user = User.Register("ana@mail.com", "stored-hash", "token-hash", s_now.AddDays(-1)).Data;
        user.Confirm("token-hash", s_now.AddDays(-1));
        return user;
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentials_IssuesASessionAndSavesTheRefreshToken()
    {
        User user = ConfirmedUser();
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Clave123!", "stored-hash").Returns(true);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.AccessToken.ShouldBe("access-token");
        result.Data.RefreshToken.ShouldBe("raw-refresh-token");
        result.Data.ExpiresInSeconds.ShouldBe(900);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id), Arg.Any<CancellationToken>());
        string[] expectedRoles = ["owner"];
        _tokenService.Received(1).IssueAccessToken(user.Id, Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(expectedRoles)));
    }

    [Fact]
    public async Task HandleAsync_VerifiedWalker_IncludesTheWalkerRole()
    {
        User user = ConfirmedUser();
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Clave123!", "stored-hash").Returns(true);
        WalkerProfile walker = WalkerProfile
            .Register(user.Id.ToString(), "Ana Pérez", DocumentType.CC, "1234567890", "3001234567", null, s_now)
            .Data;
        walker.Approve("admin-1", s_now);
        _walkerProfiles.GetByUserAsync(user.Id.ToString(), Arg.Any<CancellationToken>()).Returns(walker);

        await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        string[] expectedRoles = ["owner", "walker"];
        _tokenService.Received(1).IssueAccessToken(
            user.Id, Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(expectedRoles)));
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_FailsWithInvalidCredentials()
    {
        _users.GetByEmailAsync("ghost@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new LoginCommand("ghost@mail.com", "whatever"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_RecordsTheAttemptAndFailsWithInvalidCredentials()
    {
        User user = ConfirmedUser();
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPassword1", "stored-hash").Returns(false);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "WrongPassword1"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCredentials.Code);
        user.FailedLoginAttempts.ShouldBe(1);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountLocked_FailsWithTooManyAttemptsWithoutCheckingThePassword()
    {
        User user = ConfirmedUser();
        for (int i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.TooManyAttempts.Code);
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_UnconfirmedAccountWithCorrectPassword_FailsWithAccountNotConfirmed()
    {
        User user = User.Register("ana@mail.com", "stored-hash", "token-hash", s_now).Data;
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Clave123!", "stored-hash").Returns(true);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.AccountNotConfirmed.Code);
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
