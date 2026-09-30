using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.Entities.Walks;

public class WalkTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_luna = Guid.NewGuid();
    private static readonly Guid s_max = Guid.NewGuid();

    private static readonly FareBreakdown s_fare =
        new(Money.Of(23000m, "COP"), Money.Of(4600m, "COP"), Money.Of(18400m, "COP"));

    private static WaggoResponse<Walk> Request(
        Guid[]? petIds = null,
        string address = "Cra 7 # 45-10, Bogotá",
        DateTimeOffset? scheduledFor = null,
        string? notes = "Timbre dañado") =>
        Walk.Request(
            "owner-1",
            petIds ?? [s_luna],
            WalkType.Individual,
            WalkDuration.Create(60).Data,
            address,
            GeoPoint.Create(4.6361, -74.0645).Data,
            scheduledFor,
            notes,
            s_fare,
            s_now);

    [Fact]
    public void Request_ValidData_CreatesARequestedWalkWithTheFrozenFare()
    {
        WaggoResponse<Walk> result = Request();

        result.IsValid.ShouldBeTrue();
        Walk walk = result.Data;
        walk.Id.ShouldNotBe(Guid.Empty);
        walk.OwnerId.ShouldBe("owner-1");
        walk.PetIds.ShouldBe([s_luna]);
        walk.Status.ShouldBe(WalkStatus.Requested);
        walk.WalkType.ShouldBe(WalkType.Individual);
        walk.DurationMinutes.ShouldBe(60);
        walk.PickupAddress.ShouldBe("Cra 7 # 45-10, Bogotá");
        walk.PickupLocation.ShouldBe(GeoPoint.Create(4.6361, -74.0645).Data);
        walk.Notes.ShouldBe("Timbre dañado");
        (walk.Currency, walk.Total, walk.Commission, walk.WalkerPayout).ShouldBe(("COP", 23000m, 4600m, 18400m));
        walk.WalkerId.ShouldBeNull();
        walk.RequestedAt.ShouldBe(s_now);
    }

    [Fact]
    public void Request_WithoutSchedule_IsForNow() =>
        Request(scheduledFor: null).Data.ScheduledFor.ShouldBe(s_now);

    [Fact]
    public void Request_ScheduledWithin14Days_KeepsTheSchedule() =>
        Request(scheduledFor: s_now.AddDays(14)).Data.ScheduledFor.ShouldBe(s_now.AddDays(14));

    [Theory]
    [InlineData(-10)]    // ten minutes ago
    [InlineData(20_161)] // 14 days and 1 minute ahead
    public void Request_ScheduleOutOfRange_FailsWithInvalidSchedule(int minutesFromNow) =>
        Request(scheduledFor: s_now.AddMinutes(minutesFromNow))
            .Errors.ShouldContain(e => e.Code == WalkErrors.InvalidSchedule.Code);

    [Fact]
    public void Request_NoPets_FailsWithInvalidPets() =>
        Request(petIds: []).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidPets.Code);

    [Fact]
    public void Request_FourPets_FailsWithInvalidPets() =>
        Request(petIds: [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()])
            .Errors.ShouldContain(e => e.Code == WalkErrors.InvalidPets.Code);

    [Fact]
    public void Request_RepeatedPet_FailsWithInvalidPets() =>
        Request(petIds: [s_luna, s_luna]).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidPets.Code);

    [Fact]
    public void Request_TwoDifferentPets_Succeeds() =>
        Request(petIds: [s_luna, s_max]).Data.PetIds.ShouldBe([s_luna, s_max]);

    [Theory]
    [InlineData("Cra")]
    [InlineData("   ")]
    public void Request_ShortAddress_FailsWithInvalidAddress(string address) =>
        Request(address: address).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidAddress.Code);

    [Fact]
    public void Request_NotesLongerThan500_FailsWithInvalidNotes() =>
        Request(notes: new string('a', 501)).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidNotes.Code);

    [Fact]
    public void Request_BlankNotes_BecomeNull() =>
        Request(notes: "  ").Data.Notes.ShouldBeNull();

    [Fact]
    public void Cancel_RequestedWalk_MovesToCancelled()
    {
        Walk walk = Request().Data;

        WaggoResponse<Walk> result = walk.Cancel(s_now.AddMinutes(5));

        result.IsValid.ShouldBeTrue();
        walk.Status.ShouldBe(WalkStatus.Cancelled);
        walk.CancelledAt.ShouldBe(s_now.AddMinutes(5));
    }

    [Fact]
    public void Accept_RequestedWalk_AssignsTheWalker()
    {
        Walk walk = Request().Data;

        WaggoResponse<Walk> result = walk.Accept("walker-1", s_now.AddMinutes(2));

        result.IsValid.ShouldBeTrue();
        walk.Status.ShouldBe(WalkStatus.Accepted);
        walk.WalkerId.ShouldBe("walker-1");
        walk.AcceptedAt.ShouldBe(s_now.AddMinutes(2));
    }

    [Fact]
    public void Accept_AlreadyAccepted_FailsWithNotAvailable()
    {
        Walk walk = Request().Data;
        walk.Accept("walker-1", s_now);

        WaggoResponse<Walk> result = walk.Accept("walker-2", s_now);

        result.Errors.Single().Code.ShouldBe(WalkErrors.NotAvailable.Code);
        walk.WalkerId.ShouldBe("walker-1");
    }

    [Fact]
    public void Accept_ByTheOwner_FailsWithOwnWalk() =>
        Request().Data.Accept("owner-1", s_now).Errors.Single().Code.ShouldBe(WalkErrors.OwnWalk.Code);

    [Fact]
    public void Cancel_AcceptedWalk_MovesToCancelled()
    {
        Walk walk = Request().Data;
        walk.Accept("walker-1", s_now);

        walk.Cancel(s_now).IsValid.ShouldBeTrue();
        walk.Status.ShouldBe(WalkStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelledWalk_FailsWithCannotCancel()
    {
        Walk walk = Request().Data;
        walk.Cancel(s_now);

        WaggoResponse<Walk> result = walk.Cancel(s_now);

        result.Errors.Single().Code.ShouldBe(WalkErrors.CannotCancel.Code);
    }
}
