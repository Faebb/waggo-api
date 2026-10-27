using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.Register;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Identity.Commands.Register;

public class RegisterHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly RegisterHandler _sut;

    public RegisterHandlerTests()
    {
        _passwordHasher.Hash("Clave123!").Returns("hashed-password");
        _tokenGenerator.GenerateToken().Returns("raw-token");
        _tokenGenerator.Hash("raw-token").Returns("token-hash");
        _sut = new RegisterHandler(
            new RegisterCommandValidator(),
            _users,
            _passwordHasher,
            _tokenGenerator,
            _emailSender,
            new FixedTimeProvider());
    }

    private static RegisterCommand Command(string email = "ana@mail.com", string password = "Clave123!") =>
        new(email, password);

    [Fact]
    public async Task HandleAsync_NewEmail_SavesTheAccountAndSendsTheConfirmationEmail()
    {
        WaggoResponse<UserResponse> result = await _sut.HandleAsync(Command(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.Email.ShouldBe("ana@mail.com");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user => user.Email == "ana@mail.com" && user.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendAsync(
            "ana@mail.com", Arg.Any<string>(), Arg.Is<string>(body => body.Contains("raw-token")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailCannotBeSent_DoesNotSaveTheAccountSoTheUserCanTryAgain()
    {
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ExternalServiceException(EmailErrors.SendFailed)));

        await Should.ThrowAsync<ExternalServiceException>(() => _sut.HandleAsync(Command(), CancellationToken.None));

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailAlreadyRegistered_FailsWithoutSavingOrEmailing()
    {
        _users.ExistsByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(true);

        WaggoResponse<UserResponse> result = await _sut.HandleAsync(Command(), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.EmailAlreadyRegistered.Code);
        result.ErrorType.ShouldBe(ErrorType.BusinessRule);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("NoDigitsHere")]
    public async Task HandleAsync_WeakPassword_FailsWithoutSaving(string password)
    {
        WaggoResponse<UserResponse> result = await _sut.HandleAsync(Command(password: password), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.WeakPassword.Code);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
