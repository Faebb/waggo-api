using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Common.Models.Payments;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.FinishWalk;
using Waggo.Application.Walks.Commands.StartWalk;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Notifications;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Notifications;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Walks.Commands.StartWalk;

public class StartAndFinishWalkHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly IWalkPaymentRepository _payments = Substitute.For<IWalkPaymentRepository>();
    private readonly IPaymentGateway _gateway = PaymentMother.ApprovingGateway();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly Walk _walk = WalkMother.Requested();

    public StartAndFinishWalkHandlerTests()
    {
        _currentUser.Id.Returns("walker-1");
        _walk.Accept("walker-1", FixedTimeProvider.Default);
        _walks.GetAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(_walk);
    }

    private StartWalkHandler Start() => new(_walks, _notifications, _currentUser, new FixedTimeProvider());

    private FinishWalkHandler Finish() =>
        new(_walks, _payments, _gateway, _notifications, _currentUser, new FixedTimeProvider());

    [Fact]
    public async Task Start_AssignedWalker_StartsAndSaves()
    {
        WaggoResponse<WalkResponse> result =
            await Start().HandleAsync(new StartWalkCommand(_walk.Id), CancellationToken.None);

        result.Data.Status.ShouldBe("InProgress");
        await _walks.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_AnotherWalker_ThrowsNotFound()
    {
        _currentUser.Id.Returns("walker-2");

        await Should.ThrowAsync<NotFoundException>(
            () => Start().HandleAsync(new StartWalkCommand(_walk.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Finish_BeforeStarting_FailsWithCannotFinishWithoutSaving()
    {
        WaggoResponse<WalkResponse> result =
            await Finish().HandleAsync(new FinishWalkCommand(_walk.Id), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(WalkErrors.CannotFinish.Code);
        await _walks.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finish_StartedWalk_Completes()
    {
        await Start().HandleAsync(new StartWalkCommand(_walk.Id), CancellationToken.None);

        WaggoResponse<WalkResponse> result =
            await Finish().HandleAsync(new FinishWalkCommand(_walk.Id), CancellationToken.None);

        result.Data.Status.ShouldBe("Completed");
    }

    [Fact]
    public async Task Finish_HeldPayment_CapturesItAndPaysTheWalker()
    {
        WalkPayment payment = WalkPayment.Hold(_walk, "sim_hold_1", FixedTimeProvider.Default);
        _payments.GetByWalkAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(payment);
        await Start().HandleAsync(new StartWalkCommand(_walk.Id), CancellationToken.None);

        await Finish().HandleAsync(new FinishWalkCommand(_walk.Id), CancellationToken.None);

        payment.Status.ShouldBe(PaymentStatus.Captured);
        await _gateway.Received(1).CaptureAsync(
            new PaymentCapture("sim_hold_1", 23000m, 4600m, "walker-1", 18400m, "COP"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finish_PaidWalk_TellsTheOwnerItEndedAndTheWalkerTheyWerePaid()
    {
        _payments.GetByWalkAsync(_walk.Id, Arg.Any<CancellationToken>())
            .Returns(WalkPayment.Hold(_walk, "sim_hold_1", FixedTimeProvider.Default));
        await Start().HandleAsync(new StartWalkCommand(_walk.Id), CancellationToken.None);
        IReadOnlyList<Notification> sent = [];
        await _notifications.AddRangeAsync(
            Arg.Do<IReadOnlyList<Notification>>(list => sent = list),
            Arg.Any<CancellationToken>());

        await Finish().HandleAsync(new FinishWalkCommand(_walk.Id), CancellationToken.None);

        sent.Select(notification => (notification.UserId, notification.Kind))
            .ShouldBe([("owner-1", NotificationKind.WalkFinished), ("walker-1", NotificationKind.WalkPaid)]);
    }
}
