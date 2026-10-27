using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ConfirmEmail;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.ConfirmEmail;

/// <summary>RF-001: the owner of the inbox types the 6-digit code to activate the account.</summary>
public class ConfirmEmailHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ConfirmEmailHandler _sut;

    public ConfirmEmailHandlerTests()
    {
        _tokenGenerator.Hash("123456").Returns("code-hash");
        _tokenGenerator.Hash("654321").Returns("other-hash");
        _sut = new ConfirmEmailHandler(_users, _tokenGenerator, new FixedTimeProvider());
    }

    private User PendingUser(DateTimeOffset registeredAt)
    {
        User user = User.Register("ana@mail.com", "hash", "code-hash", registeredAt).Data;
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task HandleAsync_MatchingCode_ActivatesTheAccountAndSaves()
    {
        User user = PendingUser(s_now);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "123456"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailIsTrimmedAndLowercased_BeforeLookingTheAccountUp()
    {
        PendingUser(s_now);

        await _sut.HandleAsync(new ConfirmEmailCommand("  ANA@Mail.com ", "123456"), CancellationToken.None);

        await _users.Received(1).GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongCode_FailsWithInvalidAndSavesTheFailedAttempt()
    {
        User user = PendingUser(s_now);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "654321"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenInvalid.Code);
        user.EmailConfirmed.ShouldBeFalse();
        user.ConfirmationAttempts.ShouldBe(1);
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_FailsWithTheSameInvalidError()
    {
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "123456"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenInvalid.Code);
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountAlreadyConfirmed_FailsWithTheSameInvalidError()
    {
        User confirmed = User.Seed("ana@mail.com", "hash", isAdmin: false, s_now);
        _users.GetByEmailAsync("ana@mail.com", Arg.Any<CancellationToken>()).Returns(confirmed);

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "123456"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenInvalid.Code);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCode_FailsWithoutActivating()
    {
        User user = PendingUser(s_now.AddMinutes(-31));

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "123456"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenExpired.Code);
        user.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_RightCodeAfterTooManyWrongOnes_FailsWithAttemptsExceeded()
    {
        User user = PendingUser(s_now);
        for (int attempt = 0; attempt < User.MaxConfirmationAttempts; attempt++)
        {
            user.Confirm("other-hash", s_now);
        }

        WaggoResponse<bool> result =
            await _sut.HandleAsync(new ConfirmEmailCommand("ana@mail.com", "123456"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationAttemptsExceeded.Code);
        user.EmailConfirmed.ShouldBeFalse();
    }
}
