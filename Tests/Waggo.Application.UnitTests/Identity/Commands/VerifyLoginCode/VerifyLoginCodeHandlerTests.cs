using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.VerifyLoginCode;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.UnitTests.Identity.Commands.VerifyLoginCode;

public class VerifyLoginCodeHandlerTests
{
    private static readonly DateTimeOffset s_now = FixedTimeProvider.Default;

    private readonly ILoginChallengeRepository _challenges = Substitute.For<ILoginChallengeRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalkerProfileRepository _walkerProfiles = Substitute.For<IWalkerProfileRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly VerifyLoginCodeHandler _sut;

    public VerifyLoginCodeHandlerTests()
    {
        _tokenGenerator.Hash("048213").Returns("code-hash");
        _tokenGenerator.Hash("000000").Returns("wrong-hash");
        _tokenGenerator.GenerateToken().Returns("raw-refresh-token");
        _tokenGenerator.Hash("raw-refresh-token").Returns("refresh-hash");
        _tokenService.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(("access-token", 900));
        _sut = new VerifyLoginCodeHandler(
            _challenges, _users, _walkerProfiles, _refreshTokens, _tokenGenerator, _tokenService,
            new FixedTimeProvider());
    }

    private static User Owner() => User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;

    [Fact]
    public async Task HandleAsync_MatchingCode_IssuesASessionAndSavesTheRefreshToken()
    {
        User user = Owner();
        LoginChallenge challenge = LoginChallenge.Create(user.Id, "code-hash", s_now);
        _challenges.GetAsync(challenge.Id, Arg.Any<CancellationToken>()).Returns(challenge);
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new VerifyLoginCodeCommand(challenge.Id, "048213"), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.AccessToken.ShouldBe("access-token");
        result.Data.RefreshToken.ShouldBe("raw-refresh-token");
        result.Data.ExpiresInSeconds.ShouldBe(900);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.UserId == user.Id), Arg.Any<CancellationToken>());
        string[] expectedRoles = ["owner"];
        _tokenService.Received(1).IssueAccessToken(user.Id, Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(expectedRoles)));
    }

    [Fact]
    public async Task HandleAsync_VerifiedWalker_IncludesTheWalkerRole()
    {
        User user = Owner();
        LoginChallenge challenge = LoginChallenge.Create(user.Id, "code-hash", s_now);
        _challenges.GetAsync(challenge.Id, Arg.Any<CancellationToken>()).Returns(challenge);
        _users.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        WalkerProfile walker = WalkerProfile
            .Register(user.Id.ToString(), "Ana Pérez", DocumentType.CC, "1234567890", "3001234567", null, s_now)
            .Data;
        walker.Approve("admin-1", s_now);
        _walkerProfiles.GetByUserAsync(user.Id.ToString(), Arg.Any<CancellationToken>()).Returns(walker);

        await _sut.HandleAsync(new VerifyLoginCodeCommand(challenge.Id, "048213"), CancellationToken.None);

        string[] expectedRoles = ["owner", "walker"];
        _tokenService.Received(1).IssueAccessToken(
            user.Id, Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(expectedRoles)));
    }

    [Fact]
    public async Task HandleAsync_UnknownChallenge_FailsWithChallengeNotFound()
    {
        _challenges.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LoginChallenge?)null);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new VerifyLoginCodeCommand(Guid.NewGuid(), "048213"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ChallengeNotFound.Code);
    }

    [Fact]
    public async Task HandleAsync_WrongCode_FailsWithoutIssuingASession()
    {
        User user = Owner();
        LoginChallenge challenge = LoginChallenge.Create(user.Id, "code-hash", s_now);
        _challenges.GetAsync(challenge.Id, Arg.Any<CancellationToken>()).Returns(challenge);

        WaggoResponse<SessionResponse> result =
            await _sut.HandleAsync(new VerifyLoginCodeCommand(challenge.Id, "000000"), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCode.Code);
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _challenges.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
