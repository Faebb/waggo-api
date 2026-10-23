using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.Logout;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.Logout;

public class LogoutHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly LogoutHandler _sut;

    public LogoutHandlerTests()
    {
        _tokenGenerator.Hash("raw-token").Returns("token-hash");
        _sut = new LogoutHandler(_refreshTokens, _tokenGenerator, new FixedTimeProvider());
    }

    [Fact]
    public async Task HandleAsync_ActiveToken_RevokesItAndSucceeds()
    {
        RefreshToken existing = RefreshToken.Create(Guid.NewGuid(), "token-hash", s_now.AddDays(-1));
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(existing);

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        existing.RevokedAt.ShouldBe(s_now);
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_SucceedsWithoutChangingAnything()
    {
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyRevokedToken_SucceedsAndDoesNotRevokeTheFamily()
    {
        RefreshToken rotated = RefreshToken.Create(Guid.NewGuid(), "token-hash", s_now.AddDays(-1));
        rotated.Rotate(Guid.NewGuid(), s_now.AddHours(-1));
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(rotated);

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        rotated.RevokedAt.ShouldBe(s_now.AddHours(-1));
        await _refreshTokens.DidNotReceiveWithAnyArgs().RevokeAllForUserAsync(default, default, default);
    }
}
