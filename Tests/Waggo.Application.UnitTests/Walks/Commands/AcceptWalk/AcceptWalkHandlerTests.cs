using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.AcceptWalk;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Walks.Commands.AcceptWalk;

public class AcceptWalkHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IWalkerProfileRepository _walkerProfiles = WalkerMother.VerifiedRepository("walker-1");
    private readonly AcceptWalkHandler _sut;

    public AcceptWalkHandlerTests()
    {
        _currentUser.Id.Returns("walker-1");
        _sut = new AcceptWalkHandler(_walks, _walkerProfiles, _currentUser, new FixedTimeProvider());
    }

    [Fact]
    public async Task HandleAsync_OpenRequest_AssignsTheCurrentWalkerAndSaves()
    {
        Walk walk = WalkMother.Requested();
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(new AcceptWalkCommand(walk.Id), CancellationToken.None);

        (result.Data.Status, result.Data.WalkerId).ShouldBe(("Accepted", "walker-1"));
        await _walks.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TakenByAnotherWalker_FailsWithNotAvailableWithoutSaving()
    {
        Walk walk = WalkMother.Requested();
        walk.Accept("walker-2", FixedTimeProvider.Default);
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(new AcceptWalkCommand(walk.Id), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(WalkErrors.NotAvailable.Code);
        result.ErrorType.ShouldBe(ErrorType.Conflict);
        await _walks.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownWalk_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new AcceptWalkCommand(Guid.NewGuid()), CancellationToken.None));
}
