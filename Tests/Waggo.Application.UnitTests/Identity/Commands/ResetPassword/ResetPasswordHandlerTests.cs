using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ResetPassword;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.ResetPassword;

/// <summary>RF-001/RF-005: the 6-digit code from the email sets a new password and closes every session.</summary>
public class ResetPasswordHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ResetPasswordHandler _sut;

    public ResetPasswordHandlerTests()
    {
        _passwordHasher.Hash("Nueva1234").Returns("new-password-hash");
        _tokenGenerator.Hash("004821").Returns("code-hash");
        _tokenGenerator.Hash("999999").Returns("other-hash");
        _sut = new ResetPasswordHandler(
            new ResetPasswordCommandValidator(),
            _users,
            _passwordHasher,
            _tokenGenerator,
            _refreshTokens,
            _emailSender,
            new FixedTimeProvider());
    }

    private User UserWithCode()
    {
        User user = User.Seed("ana@mail.com", "old-hash", isAdmin: false, s_now.AddDays(-1));
        user.TryIssuePasswordResetCode("code-hash", s_now.AddMinutes(-2));
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private Task<WaggoResponse<bool>> ResetAsync(string code = "004821", string password = "Nueva1234") =>
        _sut.HandleAsync(new ResetPasswordCommand("ana@mail.com", code, password), CancellationToken.None);

    [Fact]
    public async Task HandleAsync_RightCode_ChangesThePasswordRevokesEverySessionAndWarnsByEmail()
    {
        User user = UserWithCode();

        WaggoResponse<bool> result = await ResetAsync();

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.PasswordHash.ShouldBe("new-password-hash");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, s_now, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com",
            "Cambió la contraseña de tu cuenta de Waggo",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongCode_IsInvalidSavesTheAttemptAndKeepsEverySession()
    {
        User user = UserWithCode();

        WaggoResponse<bool> result = await ResetAsync("999999");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeInvalid.Code);
        user.PasswordHash.ShouldBe("old-hash");
        user.PasswordResetAttempts.ShouldBe(1);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailWithoutAnAccount_AnswersLikeAWrongCode()
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<bool> result = await ResetAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeInvalid.Code);
    }

    [Fact]
    public async Task HandleAsync_UnconfirmedAccount_AnswersLikeAWrongCode()
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>())
            .Returns(User.Register("ana@mail.com", "hash", "confirmation-hash", s_now).Data);

        WaggoResponse<bool> result = await ResetAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeInvalid.Code);
    }

    [Theory]
    [InlineData("corta1A")]
    [InlineData("sinmayuscula1")]
    [InlineData("SinNumeroAqui")]
    public async Task HandleAsync_WeakNewPassword_IsRefusedBeforeLookingUpTheAccount(string password)
    {
        WaggoResponse<bool> result = await ResetAsync(password: password);

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.WeakPassword.Code);
        await _users.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoticeEmailFails_StillResetsThePasswordAndWarns()
    {
        User user = UserWithCode();
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed, "ses down")));

        WaggoResponse<bool> result = await ResetAsync();

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        result.Warnings.ShouldNotBeEmpty();
        user.PasswordHash.ShouldBe("new-password-hash");
    }
}
