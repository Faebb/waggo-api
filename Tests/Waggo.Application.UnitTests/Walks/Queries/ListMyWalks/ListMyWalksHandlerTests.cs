using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Queries.ListMyWalks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.UnitTests.Walks.Queries.ListMyWalks;

public class ListMyWalksHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsTheCurrentOwnersWalks()
    {
        IWalkRepository walks = Substitute.For<IWalkRepository>();
        ICurrentUser currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("owner-1");
        IReadOnlyList<Walk> stored = [WalkMother.Requested(), WalkMother.Requested()];
        walks.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns(stored);

        WaggoResponse<IReadOnlyList<WalkResponse>> result =
            await new ListMyWalksHandler(walks, currentUser).HandleAsync(new ListMyWalksQuery(), CancellationToken.None);

        result.Data.Select(walk => walk.Id).ShouldBe(stored.Select(walk => walk.Id));
    }
}
