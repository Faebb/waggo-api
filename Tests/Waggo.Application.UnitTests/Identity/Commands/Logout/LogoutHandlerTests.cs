using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.Logout;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.Logout;

/// <summary>RF-005, RNF-002: signing out revokes the refresh token on the server.</summary>
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

    private RefreshToken StoredToken(DateTimeOffset createdAt)
    {
        RefreshToken token = RefreshToken.Create(Guid.NewGuid(), "token-hash", createdAt);
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(token);
        return token;
    }

    [Fact]
    public async Task HandleAsync_ActiveToken_RevokesItAndSaves()
    {
        RefreshToken token = StoredToken(s_now.AddDays(-1));

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        token.RevokedAt.ShouldBe(s_now);
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_AnswersTheSameWithoutSaving()
    {
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyRevokedToken_AnswersTheSameAndKeepsTheFirstRevocation()
    {
        RefreshToken token = StoredToken(s_now.AddDays(-2));
        token.Revoke(s_now.AddDays(-1));

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.Data.ShouldBeTrue();
        token.RevokedAt.ShouldBe(s_now.AddDays(-1));
        await _refreshTokens.DidNotReceive().RevokeAllForUserAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_AnswersTheSame()
    {
        StoredToken(s_now.AddDays(-31));

        WaggoResponse<bool> result = await _sut.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }
}
