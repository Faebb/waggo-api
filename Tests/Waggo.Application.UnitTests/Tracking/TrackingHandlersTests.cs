using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Tracking;
using Waggo.Application.Tracking.Commands.RecordTrack;
using Waggo.Application.Tracking.Queries.GetRoute;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.UnitTests.Tracking;

public class TrackingHandlersTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly ITrackPointRepository _points = Substitute.For<ITrackPointRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Walk _walk = WalkMother.Requested(ownerId: "owner-1");

    public TrackingHandlersTests()
    {
        _walk.Accept("walker-1", FixedTimeProvider.Default);
        _walks.GetAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(_walk);
    }

    private RecordTrackHandler Record(string userId)
    {
        _currentUser.Id.Returns(userId);
        return new RecordTrackHandler(new RecordTrackCommandValidator(), _walks, _points, _currentUser);
    }

    private GetRouteHandler Route(string userId)
    {
        _currentUser.Id.Returns(userId);
        return new GetRouteHandler(_walks, _points, _currentUser, new FixedTimeProvider());
    }

    private RecordTrackCommand Batch(int count, double latitude = 4.6361) =>
        new(_walk.Id, [.. Enumerable.Range(0, count)
            .Select(i => new TrackPointInput(latitude, -74.0645, FixedTimeProvider.Default.AddMinutes(i)))]);

    [Fact]
    public async Task Record_WalkInProgress_SavesThePoints()
    {
        _walk.Start(FixedTimeProvider.Default);

        WaggoResponse<int> result = await Record("walker-1").HandleAsync(Batch(3), CancellationToken.None);

        result.Data.ShouldBe(3);
        await _points.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyList<TrackPoint>>(points => points.Count == 3 && points.All(p => p.WalkId == _walk.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Record_WalkNotStarted_FailsWithWalkNotInProgress()
    {
        WaggoResponse<int> result = await Record("walker-1").HandleAsync(Batch(1), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(TrackingErrors.WalkNotInProgress.Code);
        result.ErrorType.ShouldBe(ErrorType.BusinessRule);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Record_BatchSizeOutOfRange_FailsWithInvalidBatch(int count)
    {
        WaggoResponse<int> result = await Record("walker-1").HandleAsync(Batch(count), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(TrackingErrors.InvalidBatch.Code);
    }

    [Fact]
    public async Task Record_InvalidLatitude_FailsWithInvalidLocation()
    {
        WaggoResponse<int> result =
            await Record("walker-1").HandleAsync(Batch(1, latitude: 95), CancellationToken.None);

        result.Errors.ShouldContain(e => e.Code == WalkErrors.InvalidLocation.Code);
    }

    [Fact]
    public async Task Record_NotTheAssignedWalker_ThrowsNotFound()
    {
        _walk.Start(FixedTimeProvider.Default);

        await Should.ThrowAsync<NotFoundException>(
            () => Record("walker-2").HandleAsync(Batch(1), CancellationToken.None));
    }

    [Fact]
    public async Task Route_Owner_GetsThePointsAndMetrics()
    {
        _walk.Start(FixedTimeProvider.Default.AddMinutes(-18));
        IReadOnlyList<TrackPoint> stored =
        [
            TrackPoint.Record(_walk.Id, GeoPoint.Create(4.6405, -74.0645).Data, FixedTimeProvider.Default.AddMinutes(-5)),
            TrackPoint.Record(_walk.Id, GeoPoint.Create(4.6361, -74.0645).Data, FixedTimeProvider.Default.AddMinutes(-10)),
        ];
        _points.ListByWalkAsync(_walk.Id, Arg.Any<CancellationToken>()).Returns(stored);

        WaggoResponse<RouteResponse> result =
            await Route("owner-1").HandleAsync(new GetRouteQuery(_walk.Id), CancellationToken.None);

        result.Data.Points.Select(p => p.Latitude).ShouldBe([4.6361, 4.6405]);
        result.Data.DistanceKm.ShouldBe(0.49, 0.02);
        result.Data.ElapsedMinutes.ShouldBe(18);
    }

    [Fact]
    public async Task Route_Stranger_ThrowsNotFound() =>
        await Should.ThrowAsync<NotFoundException>(
            () => Route("owner-2").HandleAsync(new GetRouteQuery(_walk.Id), CancellationToken.None));
}
