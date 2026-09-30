using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.CancelWalk;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Walks.Commands.CancelWalk;

public class CancelWalkHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly IWalkPaymentRepository _payments = Substitute.For<IWalkPaymentRepository>();
    private readonly IPaymentGateway _gateway = PaymentMother.ApprovingGateway();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelWalkHandler _sut;

    public CancelWalkHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _sut = new CancelWalkHandler(_walks, _payments, _gateway, _currentUser, new FixedTimeProvider());
    }

    [Fact]
    public async Task HandleAsync_OwnRequestedWalk_CancelsAndSavesIt()
    {
        Walk walk = WalkMother.Requested();
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(new CancelWalkCommand(walk.Id), CancellationToken.None);

        result.Data.Status.ShouldBe("Cancelled");
        await _walks.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_FailsWithCannotCancelWithoutSaving()
    {
        Walk walk = WalkMother.Requested();
        walk.Cancel(FixedTimeProvider.Default);
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);

        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(new CancelWalkCommand(walk.Id), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(WalkErrors.CannotCancel.Code);
        result.ErrorType.ShouldBe(ErrorType.BusinessRule);
        await _walks.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalkOfAnotherOwner_ThrowsNotFound()
    {
        Walk foreign = WalkMother.Requested(ownerId: "owner-2");
        _walks.GetAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        NotFoundException exception = await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new CancelWalkCommand(foreign.Id), CancellationToken.None));

        exception.Error.ShouldBe(WalkErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_HeldPayment_ReleasesItInTheGateway()
    {
        Walk walk = WalkMother.Requested();
        WalkPayment payment = WalkPayment.Hold(walk, "sim_hold_1", FixedTimeProvider.Default);
        _walks.GetAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(walk);
        _payments.GetByWalkAsync(walk.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.HandleAsync(new CancelWalkCommand(walk.Id), CancellationToken.None);

        payment.Status.ShouldBe(PaymentStatus.Released);
        await _gateway.Received(1).ReleaseAsync("sim_hold_1", Arg.Any<CancellationToken>());
    }
}
