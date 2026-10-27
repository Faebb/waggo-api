using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ResendConfirmation;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.ResendConfirmation;

/// <summary>RF-001: a new code when the first one did not arrive, expired or ran out of attempts.</summary>
public class ResendConfirmationHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ResendConfirmationHandler _sut;

    public ResendConfirmationHandlerTests()
    {
        _tokenGenerator.GenerateNumericCode().Returns("654321");
        _tokenGenerator.Hash("654321").Returns("new-hash");
        _sut = new ResendConfirmationHandler(_users, _tokenGenerator, _emailSender, new FixedTimeProvider());
    }

    private User PendingUser(DateTimeOffset registeredAt)
    {
        User user = User.Register("ana@mail.com", "hash", "old-hash", registeredAt).Data;
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task HandleAsync_PendingAccount_SendsANewCodeAndReplacesTheOldOne()
    {
        User user = PendingUser(s_now.AddMinutes(-5));

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ResendConfirmationCommand("ana@mail.com"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.ConfirmationTokenHash.ShouldBe("new-hash");
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com",
            "Tu código de confirmación de Waggo",
            Arg.Is<string>(body => body.Contains("654321") && body.Contains("30 minutos")),
            Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BeforeTheWaitTime_AnswersTheSameWithoutSendingOrSaving()
    {
        User user = PendingUser(s_now.AddSeconds(-30));

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ResendConfirmationCommand("ana@mail.com"), CancellationToken.None);

        result.Data.ShouldBeTrue();
        user.ConfirmationTokenHash.ShouldBe("old-hash");
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_AnswersTheSameWithoutSending()
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ResendConfirmationCommand("ana@mail.com"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountAlreadyConfirmed_AnswersTheSameWithoutSending()
    {
        User confirmed = User.Seed("ana@mail.com", "hash", isAdmin: false, s_now.AddDays(-1));
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(confirmed);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ResendConfirmationCommand("ana@mail.com"), CancellationToken.None);

        result.Data.ShouldBeTrue();
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailCannotBeSent_DoesNotSaveSoTheOldCodeStillWorks()
    {
        PendingUser(s_now.AddMinutes(-5));
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed)));

        await Should.ThrowAsync<ExternalServiceException>(
            () => _sut.HandleAsync(new ResendConfirmationCommand("ana@mail.com"), CancellationToken.None));

        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
