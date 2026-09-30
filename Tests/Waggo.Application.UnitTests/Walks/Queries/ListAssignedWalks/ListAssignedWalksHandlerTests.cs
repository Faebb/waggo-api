using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Queries.ListAssignedWalks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.UnitTests.Walks.Queries.ListAssignedWalks;

public class ListAssignedWalksHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsTheWalksOfTheCurrentWalker()
    {
        IWalkRepository walks = Substitute.For<IWalkRepository>();
        ICurrentUser currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns("walker-1");
        Walk accepted = WalkMother.Requested();
        accepted.Accept("walker-1", FixedTimeProvider.Default);
        IReadOnlyList<Walk> stored = [accepted];
        walks.ListByWalkerAsync("walker-1", Arg.Any<CancellationToken>()).Returns(stored);

        WaggoResponse<IReadOnlyList<WalkResponse>> result = await new ListAssignedWalksHandler(walks, currentUser)
            .HandleAsync(new ListAssignedWalksQuery(), CancellationToken.None);

        result.Data.Select(walk => walk.Id).ShouldBe([accepted.Id]);
    }
}
