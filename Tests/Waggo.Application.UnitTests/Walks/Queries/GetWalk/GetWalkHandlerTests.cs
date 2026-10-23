using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Queries.GetWalk;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Walks.Queries.GetWalk;

public class GetWalkHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IWalkerProfileRepository _walkerProfiles = WalkerMother.VerifiedRepository("walker-1");
    private readonly GetWalkHandler _sut;

    public GetWalkHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _sut = new GetWalkHandler(_walks, _walkerProfiles, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_OwnWalk_ReturnsIt()
    {
        Walk walk = WalkMother.Requested();
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.ShouldBe(WalkResponse.From(walk, walkerProfile: null));
    }

    [Fact]
    public async Task HandleAsync_OwnWalkWithoutWalker_ReturnsWalkerNull()
    {
        Walk walk = WalkMother.Requested();
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.Walker.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_OwnAcceptedWalk_ReturnsTheAssignedWalker()
    {
        Walk walk = WalkMother.Requested();
        walk.Accept("walker-1", DateTimeOffset.UtcNow);
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.Walker.ShouldBe(new AssignedWalkerResponse("walker-1", "Andrés Gómez"));
    }

    [Fact]
    public async Task HandleAsync_WalkerWithoutProfile_ReturnsTheWalkerIdWithoutName()
    {
        Walk walk = WalkMother.Requested();
        walk.Accept("walker-legacy", DateTimeOffset.UtcNow);
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.Walker.ShouldBe(new AssignedWalkerResponse("walker-legacy", FullName: null));
    }

    [Fact]
    public async Task HandleAsync_WalkAssignedToTheCurrentWalker_ReturnsIt()
    {
        Walk walk = WalkMother.Requested(ownerId: "owner-2");
        walk.Accept("owner-1", DateTimeOffset.UtcNow); // the current user is the walker here
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.WalkerId.ShouldBe("owner-1");
    }

    [Fact]
    public async Task HandleAsync_WalkOfAnotherOwner_ThrowsNotFound()
    {
        Walk foreign = WalkMother.Requested(ownerId: "owner-2");
        _walks.GetAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new GetWalkQuery(foreign.Id), CancellationToken.None));
        await _walkerProfiles.DidNotReceiveWithAnyArgs().GetByUserAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_UnknownWalk_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new GetWalkQuery(Guid.NewGuid()), CancellationToken.None));
}
