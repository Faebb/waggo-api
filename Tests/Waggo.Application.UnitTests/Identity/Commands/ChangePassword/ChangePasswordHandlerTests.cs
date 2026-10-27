using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ChangePassword;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.ChangePassword;

/// <summary>RF-005: a signed-in account changes its password, keeps this session and closes the others.</summary>
public class ChangePasswordHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ChangePasswordHandler _sut;

    public ChangePasswordHandlerTests()
    {
        _passwordHasher.Hash("Nueva1234").Returns("new-password-hash");
        _passwordHasher.Verify("Actual123", "old-hash").Returns(true);
        _passwordHasher.Verify("Nueva1234", "old-hash").Returns(false);
        _tokenGenerator.Hash("session-token").Returns("session-hash");
        _sut = new ChangePasswordHandler(
            new ChangePasswordCommandValidator(),
            _users,
            _currentUser,
            _passwordHasher,
            _tokenGenerator,
            _refreshTokens,
            _emailSender,
            new FixedTimeProvider());
    }

    private User SignedInUser()
    {
        User user = User.Seed("ana@mail.com", "old-hash", isAdmin: false, s_now.AddDays(-1));
        _currentUser.Id.Returns(user.Id.ToString());
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private RefreshToken StoredSessionToken(Guid userId)
    {
        RefreshToken token = RefreshToken.Create(userId, "session-hash", s_now.AddHours(-1));
        _refreshTokens.GetByTokenHashAsync("session-hash", Arg.Any<CancellationToken>()).Returns(token);
        return token;
    }

    private Task<WaggoResponse<bool>> ChangeAsync(
        string current = "Actual123",
        string password = "Nueva1234",
        string? refreshToken = "session-token") =>
        _sut.HandleAsync(new ChangePasswordCommand(current, password, refreshToken), CancellationToken.None);

    [Fact]
    public async Task HandleAsync_RightCurrentPassword_ChangesItKeepsThisSessionAndWarnsByEmail()
    {
        User user = SignedInUser();
        RefreshToken session = StoredSessionToken(user.Id);

        WaggoResponse<bool> result = await ChangeAsync();

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.PasswordHash.ShouldBe("new-password-hash");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).RevokeAllForUserExceptAsync(
            user.Id, session.Id, s_now, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com",
            "Cambió la contraseña de tu cuenta de Waggo",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoSessionTokenGiven_ClosesEverySession()
    {
        User user = SignedInUser();

        WaggoResponse<bool> result = await ChangeAsync(refreshToken: null);

        result.IsValid.ShouldBeTrue();
        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, s_now, Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().RevokeAllForUserExceptAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SessionTokenOfAnotherAccount_ClosesEverySessionOfThisOne()
    {
        User user = SignedInUser();
        StoredSessionToken(Guid.NewGuid());

        await ChangeAsync();

        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, s_now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongCurrentPassword_IsRefusedAndCountsAFailedAttempt()
    {
        User user = SignedInUser();

        WaggoResponse<bool> result = await ChangeAsync(current: "Equivocada1");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.CurrentPasswordIncorrect.Code);
        user.PasswordHash.ShouldBe("old-hash");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TooManyWrongCurrentPasswords_LocksTheAccountEvenForTheRightOne()
    {
        User user = SignedInUser();
        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            await ChangeAsync(current: "Equivocada1");
        }

        WaggoResponse<bool> result = await ChangeAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.TooManyAttempts.Code);
        user.PasswordHash.ShouldBe("old-hash");
    }

    [Fact]
    public async Task HandleAsync_NewPasswordEqualToTheCurrentOne_IsRefused()
    {
        SignedInUser();
        _passwordHasher.Verify("Actual123", "old-hash").Returns(true);

        WaggoResponse<bool> result = await ChangeAsync(current: "Actual123", password: "Actual123");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.PasswordUnchanged.Code);
    }

    [Theory]
    [InlineData("corta1A")]
    [InlineData("sinmayuscula1")]
    [InlineData("SinNumeroAqui")]
    public async Task HandleAsync_WeakNewPassword_IsRefusedBeforeLookingUpTheAccount(string password)
    {
        WaggoResponse<bool> result = await ChangeAsync(password: password);

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.WeakPassword.Code);
        await _users.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoticeEmailFails_StillChangesThePasswordAndWarns()
    {
        User user = SignedInUser();
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed, "ses down")));

        WaggoResponse<bool> result = await ChangeAsync();

        result.IsValid.ShouldBeTrue();
        result.Warnings.ShouldNotBeEmpty();
        user.PasswordHash.ShouldBe("new-password-hash");
    }
}
