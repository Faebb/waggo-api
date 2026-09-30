using Waggo.Application.Common.Interfaces;
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
    private readonly GetWalkHandler _sut;

    public GetWalkHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _sut = new GetWalkHandler(_walks, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_OwnWalk_ReturnsIt()
    {
        Walk walk = WalkMother.Requested();
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(new GetWalkQuery(walk.Id), CancellationToken.None);

        result.Data.ShouldBe(WalkResponse.From(walk));
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
    }

    [Fact]
    public async Task HandleAsync_UnknownWalk_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new GetWalkQuery(Guid.NewGuid()), CancellationToken.None));
}
