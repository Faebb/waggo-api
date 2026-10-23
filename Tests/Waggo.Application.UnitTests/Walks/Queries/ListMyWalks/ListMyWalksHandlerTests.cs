using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Queries.ListMyWalks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.UnitTests.Walks.Queries.ListMyWalks;

public class ListMyWalksHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsTheCurrentOwnersWalksWithTheirWalkers()
    {
        IWalkRepository walks = Substitute.For<IWalkRepository>();
        ICurrentUser currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("owner-1");
        Walk accepted = WalkMother.Requested();
        accepted.Accept("walker-1", FixedTimeProvider.Default);
        IReadOnlyList<Walk> stored = [accepted, WalkMother.Requested()];
        walks.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns(stored);
        IWalkerProfileRepository walkerProfiles = Substitute.For<IWalkerProfileRepository>();
        walkerProfiles
            .ListByUsersAsync(
                Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 1 && ids.Contains("walker-1")),
                Arg.Any<CancellationToken>())
            .Returns([WalkerMother.Approved("walker-1")]);

        WaggoResponse<IReadOnlyList<WalkResponse>> result =
            await new ListMyWalksHandler(walks, walkerProfiles, currentUser)
                .HandleAsync(new ListMyWalksQuery(), CancellationToken.None);

        result.Data.Select(walk => walk.Id).ShouldBe(stored.Select(walk => walk.Id));
        result.Data[0].Walker.ShouldBe(new AssignedWalkerResponse("walker-1", "Andrés Gómez"));
        result.Data[1].Walker.ShouldBeNull();
    }
}
