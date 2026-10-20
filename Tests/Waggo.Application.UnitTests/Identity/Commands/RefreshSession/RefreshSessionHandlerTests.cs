using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.RefreshSession;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.RefreshSession;

public class RefreshSessionHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalkerProfileRepository _walkerProfiles = Substitute.For<IWalkerProfileRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly RefreshSessionHandler _sut;

    public RefreshSessionHandlerTests()
    {
        _tokenGenerator.Hash("raw-token").Returns("token-hash");
        _tokenGenerator.GenerateToken().Returns("new-raw-token");
        _tokenGenerator.Hash("new-raw-token").Returns("new-token-hash");
        _tokenService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(("new-access-token", 900));
        _sut = new RefreshSessionHandler(
            _refreshTokens, _users, _walkerProfiles, _tokenGenerator, _tokenService, new FixedTimeProvider());
    }

    private static User Owner() => User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;

    [Fact]
    public async Task HandleAsync_ActiveToken_RotatesItAndIssuesANewSession()
    {
        User user = Owner();
        RefreshToken existing = RefreshToken.Create(user.Id, "token-hash", s_now);
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(existing);
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new RefreshSessionCommand("raw-token"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.AccessToken.ShouldBe("new-access-token");
        result.Data.RefreshToken.ShouldBe("new-raw-token");
        existing.RevokedAt.ShouldBe(s_now);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == "new-token-hash"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_FailsWithRefreshTokenInvalid()
    {
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new RefreshSessionCommand("raw-token"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_FailsWithRefreshTokenInvalid()
    {
        User user = Owner();
        RefreshToken expired = RefreshToken.Create(user.Id, "token-hash", s_now.Subtract(RefreshToken.Lifetime).AddSeconds(-1));
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(expired);
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new RefreshSessionCommand("raw-token"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
    }

    [Fact]
    public async Task HandleAsync_AlreadyRotatedToken_RevokesTheWholeFamilyAndFails()
    {
        User user = Owner();
        RefreshToken reused = RefreshToken.Create(user.Id, "token-hash", s_now);
        reused.Rotate(Guid.NewGuid(), s_now.AddMinutes(-1));
        _refreshTokens.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(reused);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new RefreshSessionCommand("raw-token"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.RefreshTokenInvalid.Code);
        await _refreshTokens.Received(1).RevokeAllForUserAsync(user.Id, s_now, Arg.Any<CancellationToken>());
    }
}
