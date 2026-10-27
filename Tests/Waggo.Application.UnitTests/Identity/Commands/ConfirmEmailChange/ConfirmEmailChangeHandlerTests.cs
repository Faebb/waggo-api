using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ConfirmEmailChange;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.ConfirmEmailChange;

/// <summary>RF-005: the code from the new inbox applies the new email and closes the other sessions.</summary>
public class ConfirmEmailChangeHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ConfirmEmailChangeHandler _sut;

    public ConfirmEmailChangeHandlerTests()
    {
        _tokenGenerator.Hash("004821").Returns("code-hash");
        _tokenGenerator.Hash("999999").Returns("other-hash");
        _tokenGenerator.Hash("session-token").Returns("session-hash");
        _sut = new ConfirmEmailChangeHandler(
            _users, _currentUser, _tokenGenerator, _refreshTokens, _emailSender, new FixedTimeProvider());
    }

    private User UserWithPendingChange()
    {
        User user = User.Seed("ana@mail.com", "password-hash", isAdmin: false, s_now.AddDays(-1));
        user.RequestEmailChange("ana.nueva@mail.com", "code-hash", s_now.AddMinutes(-2));
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

    private Task<WaggoResponse<bool>> ConfirmAsync(string code = "004821", string? refreshToken = "session-token") =>
        _sut.HandleAsync(new ConfirmEmailChangeCommand(code, refreshToken), CancellationToken.None);

    [Fact]
    public async Task HandleAsync_RightCode_AppliesTheEmailKeepsThisSessionAndWarnsTheOldAddress()
    {
        User user = UserWithPendingChange();
        RefreshToken session = StoredSessionToken(user.Id);

        WaggoResponse<bool> result = await ConfirmAsync();

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.Email.ShouldBe("ana.nueva@mail.com");
        await _refreshTokens.Received(1).RevokeAllForUserExceptAsync(
            user.Id, session.Id, s_now, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com",
            "Cambió el correo de tu cuenta de Waggo",
            Arg.Is<string>(body => body.Contains("ana.nueva@mail.com")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoSessionTokenGiven_ClosesEverySession()
    {
        User user = UserWithPendingChange();

        await ConfirmAsync(refreshToken: null);

        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, s_now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongCode_IsInvalidSavesTheAttemptAndChangesNothing()
    {
        User user = UserWithPendingChange();

        WaggoResponse<bool> result = await ConfirmAsync("999999");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailChangeCodeInvalid.Code);
        user.Email.ShouldBe("ana@mail.com");
        user.EmailChangeAttempts.ShouldBe(1);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NewEmailTakenByAnotherAccountMeanwhile_IsRefused()
    {
        User user = UserWithPendingChange();
        _users.ExistsByEmailAsync("ana.nueva@mail.com", Arg.Any<CancellationToken>()).Returns(true);

        WaggoResponse<bool> result = await ConfirmAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailAlreadyRegistered.Code);
        user.Email.ShouldBe("ana@mail.com");
    }

    [Fact]
    public async Task HandleAsync_NoticeEmailFails_StillAppliesTheEmailAndWarns()
    {
        User user = UserWithPendingChange();
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed, "ses down")));

        WaggoResponse<bool> result = await ConfirmAsync();

        result.IsValid.ShouldBeTrue();
        result.Warnings.ShouldNotBeEmpty();
        user.Email.ShouldBe("ana.nueva@mail.com");
    }
}
