using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Notifications;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Tracking;
using Waggo.Application.Tracking.Commands.RaiseEmergency;
using Waggo.Application.Tracking.Queries.ListWalkAlerts;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Notifications;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Enums.Notifications;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Tracking;

public class AlertHandlersTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly IWalkAlertRepository _alerts = Substitute.For<IWalkAlertRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly Walk _walk = WalkMother.Requested(ownerId: "owner-1");

    public AlertHandlersTests() => _walks.GetAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(_walk);

    private RaiseEmergencyHandler Raise(string userId)
    {
        _currentUser.Id.Returns(userId);
        return new RaiseEmergencyHandler(_walks, _alerts, _notifications, _currentUser, new FixedTimeProvider());
    }

    private static DateTimeOffset Later(int minutes) => FixedTimeProvider.Default.AddMinutes(minutes);

    private RaiseEmergencyCommand Command(string? message = "Luna se soltó") => new(_walk.Id, message, 4.64, -74.062);

    [Fact]
    public async Task Raise_WalkerOfAnAcceptedWalk_SavesAWalkerEmergency()
    {
        _walk.Accept("walker-1", FixedTimeProvider.Default);

        WaggoResponse<WalkAlertResponse> result =
            await Raise("walker-1").HandleAsync(Command(), CancellationToken.None);

        (result.Data.Kind, result.Data.RaisedBy).ShouldBe(("Emergency", "Walker"));
        await _alerts.Received(1).AddAsync(
            Arg.Is<WalkAlert>(alert => alert.RaisedBy == WalkParty.Walker && alert.WalkId == _walk.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Raise_Owner_SavesAnOwnerEmergency()
    {
        _walk.Accept("walker-1", FixedTimeProvider.Default);

        WaggoResponse<WalkAlertResponse> result = await Raise("owner-1").HandleAsync(Command(), CancellationToken.None);

        result.Data.RaisedBy.ShouldBe("Owner");
    }

    [Fact]
    public async Task Raise_WalkNotAcceptedYet_FailsWithWalkNotActive()
    {
        WaggoResponse<WalkAlertResponse> result = await Raise("owner-1").HandleAsync(Command(), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(AlertErrors.WalkNotActive.Code);
        result.ErrorType.ShouldBe(ErrorType.BusinessRule);
        await _alerts.DidNotReceive().AddAsync(Arg.Any<WalkAlert>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Raise_Stranger_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => Raise("someone-else").HandleAsync(Command(), CancellationToken.None));

    [Fact]
    public async Task List_Owner_GetsTheAlertsNewestFirst()
    {
        _currentUser.Id.Returns("owner-1");
        IReadOnlyList<WalkAlert> stored =
        [
            WalkAlert.RaiseEmergency(_walk.Id, WalkParty.Walker, "primera", null, FixedTimeProvider.Default).Data,
            WalkAlert.RaiseEmergency(_walk.Id, WalkParty.Owner, "segunda", null, Later(minutes: 1)).Data,
        ];
        _alerts.ListByWalkAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(stored);

        ListWalkAlertsHandler sut = new(_walks, _alerts, _currentUser);

        WaggoResponse<IReadOnlyList<WalkAlertResponse>> result =
            await sut.HandleAsync(new ListWalkAlertsQuery(_walk.Id), CancellationToken.None);

        result.Data.Select(alert => alert.Message).ShouldBe(["segunda", "primera"]);
    }

    [Fact]
    public async Task Raise_ByTheWalker_TellsTheOwnerWithHighPriority()
    {
        _walk.Accept("walker-1", FixedTimeProvider.Default);

        await Raise("walker-1").HandleAsync(Command(), CancellationToken.None);

        await _notifications.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyList<Notification>>(list => list.Single().UserId == "owner-1"
                && list.Single().Kind == NotificationKind.Emergency
                && list.Single().Priority == NotificationPriority.High),
            Arg.Any<CancellationToken>());
    }
}
