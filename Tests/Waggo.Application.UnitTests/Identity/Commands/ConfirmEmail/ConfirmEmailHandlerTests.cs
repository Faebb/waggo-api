using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.ConfirmEmail;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.ConfirmEmail;

public class ConfirmEmailHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ConfirmEmailHandler _sut;

    public ConfirmEmailHandlerTests()
    {
        _tokenGenerator.Hash("raw-token").Returns("token-hash");
        _sut = new ConfirmEmailHandler(_users, _tokenGenerator, new FixedTimeProvider());
    }

    [Fact]
    public async Task HandleAsync_MatchingToken_ActivatesTheAccountAndSaves()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;
        _users.GetByConfirmationTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<bool> result = await _sut.HandleAsync(new ConfirmEmailCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TokenNotFound_FailsWithInvalidToken()
    {
        _users.GetByConfirmationTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns((User?)null);

        WaggoResponse<bool> result = await _sut.HandleAsync(new ConfirmEmailCommand("raw-token"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenInvalid.Code);
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_FailsWithoutActivating()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now.AddHours(-25)).Data;
        _users.GetByConfirmationTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<bool> result = await _sut.HandleAsync(new ConfirmEmailCommand("raw-token"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenExpired.Code);
        user.EmailConfirmed.ShouldBeFalse();
        await _users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
