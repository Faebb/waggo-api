using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.Login;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.Login;

public class LoginHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ILoginChallengeRepository _challenges = Substitute.For<ILoginChallengeRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _tokenGenerator.GenerateNumericCode().Returns("048213");
        _tokenGenerator.Hash("048213").Returns("code-hash");
        _sut = new LoginHandler(
            _users, _challenges, _passwordHasher, _tokenGenerator, _emailSender, new FixedTimeProvider());
    }

    private static User ConfirmedUser()
    {
        User user = User.Register("ana@mail.com", "stored-hash", "token-hash", s_now.AddDays(-1)).Data;
        user.Confirm("token-hash", s_now.AddDays(-1));
        return user;
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentials_CreatesAChallengeAndEmailsTheCode()
    {
        User user = ConfirmedUser();
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Clave123!", "stored-hash").Returns(true);

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        await _challenges.Received(1).AddAsync(
            Arg.Is<LoginChallenge>(c => c.UserId == user.Id && c.Id == result.Data.ChallengeId),
            Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com", Arg.Any<string>(), Arg.Is<string>(body => body.Contains("048213")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_FailsWithInvalidCredentials()
    {
        _users.GetByEmailAsync("ghost@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new LoginCommand("ghost@mail.com", "whatever"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_RecordsTheAttemptAndFailsWithInvalidCredentials()
    {
        User user = ConfirmedUser();
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPassword1", "stored-hash").Returns(false);

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "WrongPassword1"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCredentials.Code);
        user.FailedLoginAttempts.ShouldBe(1);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _challenges.DidNotReceive().AddAsync(Arg.Any<LoginChallenge>(), Arg.Any<CancellationToken>());
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

        WaggoResponse<LoginResponse> result =
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

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new LoginCommand("ana@mail.com", "Clave123!"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.AccountNotConfirmed.Code);
        await _challenges.DidNotReceive().AddAsync(Arg.Any<LoginChallenge>(), Arg.Any<CancellationToken>());
    }
}
