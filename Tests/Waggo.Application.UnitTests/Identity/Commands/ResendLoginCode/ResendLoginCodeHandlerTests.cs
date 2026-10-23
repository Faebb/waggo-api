using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.ResendLoginCode;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.ResendLoginCode;

public class ResendLoginCodeHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly ILoginChallengeRepository _challenges = Substitute.For<ILoginChallengeRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ResendLoginCodeHandler _sut;

    public ResendLoginCodeHandlerTests()
    {
        _tokenGenerator.GenerateNumericCode().Returns("731905");
        _tokenGenerator.Hash("731905").Returns("new-code-hash");
        _sut = new ResendLoginCodeHandler(
            _challenges, _users, _tokenGenerator, _emailSender, new FixedTimeProvider());
    }

    private static User ConfirmedUser()
    {
        User user = User.Register("ana@mail.com", "stored-hash", "token-hash", s_now.AddDays(-1)).Data;
        user.Confirm("token-hash", s_now.AddDays(-1));
        return user;
    }

    private LoginChallenge PendingChallengeOf(User user)
    {
        LoginChallenge challenge = LoginChallenge.Create(user.Id, "old-code-hash", s_now.AddMinutes(-3));
        _challenges.GetAsync(challenge.Id, Arg.Any<CancellationToken>()).Returns(challenge);
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return challenge;
    }

    [Fact]
    public async Task HandleAsync_PendingChallenge_ReplacesTheCodeAndEmailsTheNewOne()
    {
        User user = ConfirmedUser();
        LoginChallenge challenge = PendingChallengeOf(user);

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new ResendLoginCodeCommand(challenge.Id), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ChallengeId.ShouldBe(challenge.Id);
        challenge.CodeHash.ShouldBe("new-code-hash");
        await _challenges.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com", Arg.Any<string>(), Arg.Is<string>(body => body.Contains("731905")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownChallenge_FailsWithChallengeNotFoundAndSendsNothing()
    {
        _challenges.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LoginChallenge?)null);

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new ResendLoginCodeCommand(Guid.NewGuid()), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ChallengeNotFound.Code);
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_AttemptsExhausted_FailsWithTooManyAttemptsAndSendsNothing()
    {
        User user = ConfirmedUser();
        LoginChallenge challenge = PendingChallengeOf(user);
        for (int i = 0; i < LoginChallenge.MaxAttempts - 1; i++)
        {
            challenge.Verify("wrong-hash", s_now);
        }

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new ResendLoginCodeCommand(challenge.Id), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.TooManyAttempts.Code);
        await _challenges.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_LockedAccount_FailsWithTooManyAttemptsAndSendsNothing()
    {
        User user = ConfirmedUser();
        LoginChallenge challenge = PendingChallengeOf(user);
        for (int i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        WaggoResponse<LoginResponse> result =
            await _sut.HandleAsync(new ResendLoginCodeCommand(challenge.Id), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.TooManyAttempts.Code);
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }
}
