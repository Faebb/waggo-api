using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ForgotPassword;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.ForgotPassword;

/// <summary>RF-001/RF-005: asks for a 6-digit code to set a new password, revealing nothing about the account.</summary>
public class ForgotPasswordHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ForgotPasswordHandler _sut;

    public ForgotPasswordHandlerTests()
    {
        _tokenGenerator.GenerateNumericCode().Returns("654321");
        _tokenGenerator.Hash("654321").Returns("new-hash");
        _sut = new ForgotPasswordHandler(_users, _tokenGenerator, _emailSender, new FixedTimeProvider());
    }

    private User ExistingUser(User user)
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private static User ConfirmedUser() => User.Seed("ana@mail.com", "hash", isAdmin: false, s_now.AddDays(-1));

    private async Task AssertNothingWasSentOrSavedAsync()
    {
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConfirmedAccount_EmailsTheCodeAndSavesItsHash()
    {
        User user = ExistingUser(ConfirmedUser());

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ForgotPasswordCommand("Ana@Mail.com"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.PasswordResetCodeHash.ShouldBe("new-hash");
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com",
            "Tu código para restablecer la contraseña de Waggo",
            Arg.Is<string>(body => body.Contains("654321") && body.Contains("15 minutos")),
            Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailWithoutAnAccount_AnswersTheSameAndSendsNothing()
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ForgotPasswordCommand("ana@mail.com"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        await AssertNothingWasSentOrSavedAsync();
    }

    [Fact]
    public async Task HandleAsync_UnconfirmedAccount_AnswersTheSameAndSendsNothing()
    {
        ExistingUser(User.Register("ana@mail.com", "hash", "confirmation-hash", s_now.AddDays(-1)).Data);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ForgotPasswordCommand("ana@mail.com"), CancellationToken.None);

        result.Data.ShouldBeTrue();
        await AssertNothingWasSentOrSavedAsync();
    }

    [Fact]
    public async Task HandleAsync_BeforeTheWaitTime_AnswersTheSameWithoutSendingOrSaving()
    {
        User user = ConfirmedUser();
        user.TryIssuePasswordResetCode("old-hash", s_now.AddSeconds(-30));
        ExistingUser(user);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ForgotPasswordCommand("ana@mail.com"), CancellationToken.None);

        result.Data.ShouldBeTrue();
        user.PasswordResetCodeHash.ShouldBe("old-hash");
        await AssertNothingWasSentOrSavedAsync();
    }

    [Fact]
    public async Task HandleAsync_SecondRequestAfterTheWaitTime_ReplacesTheCode()
    {
        User user = ConfirmedUser();
        user.TryIssuePasswordResetCode("old-hash", s_now.AddMinutes(-2));
        ExistingUser(user);

        await _sut.HandleAsync(new ForgotPasswordCommand("ana@mail.com"), CancellationToken.None);

        user.PasswordResetCodeHash.ShouldBe("new-hash");
    }

    [Fact]
    public async Task HandleAsync_EmailProviderFails_ThrowsAndSavesNothing()
    {
        ExistingUser(ConfirmedUser());
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(
                Domain.Errors.Identity.EmailErrors.SendFailed, "ses down")));

        await Should.ThrowAsync<ExternalServiceException>(() =>
            _sut.HandleAsync(new ForgotPasswordCommand("ana@mail.com"), CancellationToken.None));

        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
