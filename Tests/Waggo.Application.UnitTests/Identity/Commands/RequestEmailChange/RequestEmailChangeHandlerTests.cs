using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.RequestEmailChange;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.RequestEmailChange;

/// <summary>RF-005: asks to change the email; a code goes to the NEW inbox and nothing changes until it is typed.</summary>
public class RequestEmailChangeHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly RequestEmailChangeHandler _sut;

    public RequestEmailChangeHandlerTests()
    {
        _passwordHasher.Verify("Clave123!", "password-hash").Returns(true);
        _tokenGenerator.GenerateNumericCode().Returns("654321");
        _tokenGenerator.Hash("654321").Returns("code-hash");
        _sut = new RequestEmailChangeHandler(
            _users, _currentUser, _passwordHasher, _tokenGenerator, _emailSender, new FixedTimeProvider());
    }

    private User SignedInUser()
    {
        User user = User.Seed("ana@mail.com", "password-hash", isAdmin: false, s_now.AddDays(-1));
        _currentUser.Id.Returns(user.Id.ToString());
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private Task<WaggoResponse<bool>> RequestAsync(string newEmail = "Ana.Nueva@Mail.com", string password = "Clave123!") =>
        _sut.HandleAsync(new RequestEmailChangeCommand(newEmail, password), CancellationToken.None);

    private async Task AssertNothingSentOrSavedAsync()
    {
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FreeEmailAndRightPassword_EmailsTheCodeToTheNewAddressAndKeepsTheOldOne()
    {
        User user = SignedInUser();

        WaggoResponse<bool> result = await RequestAsync();

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.Email.ShouldBe("ana@mail.com");
        user.PendingEmail.ShouldBe("ana.nueva@mail.com");
        user.EmailChangeCodeHash.ShouldBe("code-hash");
        await _emailSender.Received(1).SendAsync(
            "ana.nueva@mail.com",
            "Tu código para confirmar el nuevo correo de Waggo",
            Arg.Is<string>(body => body.Contains("654321") && body.Contains("15 minutos")),
            Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_IsRefusedAndCountsAFailedAttempt()
    {
        User user = SignedInUser();
        _passwordHasher.Verify("Equivocada1", "password-hash").Returns(false);

        WaggoResponse<bool> result = await RequestAsync(password: "Equivocada1");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.CurrentPasswordIncorrect.Code);
        user.PendingEmail.ShouldBeNull();
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TooManyWrongPasswords_LocksTheAccountEvenForTheRightOne()
    {
        SignedInUser();
        _passwordHasher.Verify("Equivocada1", "password-hash").Returns(false);
        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            await RequestAsync(password: "Equivocada1");
        }

        WaggoResponse<bool> result = await RequestAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.TooManyAttempts.Code);
    }

    [Fact]
    public async Task HandleAsync_EmailAlreadyUsedByAnotherAccount_IsRefusedWithoutSending()
    {
        SignedInUser();
        _users.ExistsByEmailAsync("ana.nueva@mail.com", Arg.Any<CancellationToken>()).Returns(true);

        WaggoResponse<bool> result = await RequestAsync();

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailAlreadyRegistered.Code);
        await AssertNothingSentOrSavedAsync();
    }

    [Fact]
    public async Task HandleAsync_SameEmailAsTheCurrentOne_IsRefused()
    {
        SignedInUser();

        WaggoResponse<bool> result = await RequestAsync(newEmail: "ANA@mail.com");

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailUnchanged.Code);
        await AssertNothingSentOrSavedAsync();
    }

    [Fact]
    public async Task HandleAsync_EmailProviderFails_ThrowsAndSavesNothing()
    {
        User user = SignedInUser();
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed, "ses down")));

        await Should.ThrowAsync<ExternalServiceException>(() => RequestAsync());

        user.PendingEmail.ShouldBeNull();
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
