using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Queries.ListAvailableWalks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.UnitTests.Walks.Queries.ListAvailableWalks;

public class ListAvailableWalksHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ListAvailableWalksHandler _sut;

    public ListAvailableWalksHandlerTests()
    {
        _currentUser.Id.Returns("walker-1");
        _sut = new ListAvailableWalksHandler(new ListAvailableWalksQueryValidator(), _walks, _currentUser);
    }

    private static Walk RequestedAt(string ownerId, double latitude, double longitude, int hoursAhead) =>
        Walk.Request(
            ownerId,
            [Guid.NewGuid(), Guid.NewGuid()],
            WalkType.Individual,
            WalkDuration.Create(60).Data,
            "Cra 7 # 45-10, Bogotá",
            GeoPoint.Create(latitude, longitude).Data,
            FixedTimeProvider.Default.AddHours(hoursAhead),
            "Timbre dañado",
            new FareBreakdown(Money.Of(23000m, "COP"), Money.Of(4600m, "COP"), Money.Of(18400m, "COP")),
            FixedTimeProvider.Default).Data;

    [Fact]
    public async Task HandleAsync_WithoutLocation_ListsOthersRequestsBySchedule_WithThePayout()
    {
        Walk later = RequestedAt("owner-1", 4.68, -74.06, hoursAhead: 5);
        Walk sooner = RequestedAt("owner-2", 4.64, -74.06, hoursAhead: 1);
        Walk mine = RequestedAt("walker-1", 4.64, -74.06, hoursAhead: 1);
        IReadOnlyList<Walk> requested = [later, sooner, mine];
        _walks.ListRequestedAsync(Arg.Any<CancellationToken>()).Returns(requested);

        WaggoResponse<IReadOnlyList<AvailableWalkResponse>> result =
            await _sut.HandleAsync(new ListAvailableWalksQuery(null, null), CancellationToken.None);

        result.Data.Select(offer => offer.Id).ShouldBe([sooner.Id, later.Id]);
        AvailableWalkResponse first = result.Data[0];
        (first.PetCount, first.WalkerPayout, first.DistanceKm).ShouldBe((2, 18400m, (double?)null));
    }

    [Fact]
    public async Task HandleAsync_WithLocation_ListsTheNearestFirst()
    {
        Walk far = RequestedAt("owner-1", 4.68, -74.06, hoursAhead: 1);
        Walk near = RequestedAt("owner-2", 4.645, -74.06, hoursAhead: 5);
        IReadOnlyList<Walk> requested = [far, near];
        _walks.ListRequestedAsync(Arg.Any<CancellationToken>()).Returns(requested);

        WaggoResponse<IReadOnlyList<AvailableWalkResponse>> result =
            await _sut.HandleAsync(new ListAvailableWalksQuery(4.6361, -74.0645), CancellationToken.None);

        result.Data.Select(offer => offer.Id).ShouldBe([near.Id, far.Id]);
        result.Data[0].DistanceKm!.Value.ShouldBe(1.1, 0.2);
    }

    [Theory]
    [InlineData(95.0, 0.0)]
    [InlineData(4.6, null)]
    public async Task HandleAsync_InvalidLocation_FailsWithInvalidLocation(double? latitude, double? longitude)
    {
        WaggoResponse<IReadOnlyList<AvailableWalkResponse>> result =
            await _sut.HandleAsync(new ListAvailableWalksQuery(latitude, longitude), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(WalkErrors.InvalidLocation.Code);
    }
}
