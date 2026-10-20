using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Common.Interfaces.Pricing;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Common.Models.Payments;
using Waggo.Application.UnitTests.TestData;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Errors.Payments;
using Waggo.Domain.Errors.Pricing;
using Waggo.Domain.Errors.Walks;

namespace Waggo.Application.UnitTests.Walks.Commands.RequestWalk;

public class RequestWalkHandlerTests
{
    private readonly IWalkRepository _walks = Substitute.For<IWalkRepository>();
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly IPricingTableProvider _pricing = Substitute.For<IPricingTableProvider>();
    private readonly IWalkPaymentRepository _payments = Substitute.For<IWalkPaymentRepository>();
    private readonly IPaymentGateway _gateway = PaymentMother.ApprovingGateway();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Pet _luna = PetMother.Luna();
    private readonly Pet _max = PetMother.Max();
    private readonly RequestWalkHandler _sut;

    public RequestWalkHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        IReadOnlyList<Pet> mine = [_luna, _max];
        _pets.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns(mine);
        _pricing.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(WalkMother.PricingResponse());

        _sut = new RequestWalkHandler(
            new RequestWalkCommandValidator(),
            _walks,
            _pets,
            _pricing,
            _payments,
            _gateway,
            _currentUser,
            new FixedTimeProvider());
    }

    private RequestWalkCommand Command(
        Guid[]? petIds = null,
        WalkType walkType = WalkType.Individual,
        int durationMinutes = 60) =>
        new(
            petIds ?? [_luna.Id],
            walkType,
            durationMinutes,
            "Cra 7 # 45-10, Bogotá",
            4.6361,
            -74.0645,
            null,
            "Timbre dañado");

    [Fact]
    public async Task HandleAsync_ValidCommand_SavesARequestedWalkWithTheQuotedFare()
    {
        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(Command(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.Status.ShouldBe("Requested");
        (result.Data.Total, result.Data.Commission, result.Data.WalkerPayout).ShouldBe((23000m, 4600m, 18400m));
        result.Data.ScheduledFor.ShouldBe(FixedTimeProvider.Default);
        await _walks.Received(1).AddAsync(
            Arg.Is<Walk>(walk => walk.OwnerId == "owner-1" && walk.Id == result.Data.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GroupWalkWithTwoDogs_UsesTheGroupRate()
    {
        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(
            Command(petIds: [_luna.Id, _max.Id], walkType: WalkType.Group, durationMinutes: 45),
            CancellationToken.None);

        result.Data.Total.ShouldBe(11800m);
    }

    [Fact]
    public async Task HandleAsync_PetOfAnotherOwner_FailsWithInvalidPetsWithoutSaving()
    {
        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(Command(petIds: [Guid.NewGuid()]), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(WalkErrors.InvalidPets.Code);
        await _walks.DidNotReceive().AddAsync(Arg.Any<Walk>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidDuration_FailsWithThePricingCodeWithoutLoadingPets()
    {
        WaggoResponse<WalkResponse> result =
            await _sut.HandleAsync(Command(durationMinutes: 50), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(PricingErrors.InvalidDuration.Code);
        await _pets.DidNotReceive().ListByOwnerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FourPets_FailsWithInvalidPets()
    {
        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(
            Command(petIds: [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]),
            CancellationToken.None);

        result.Errors.ShouldContain(e => e.Code == WalkErrors.InvalidPets.Code);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_HoldsTheTotalAndSavesTheHeldPayment()
    {
        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(Command(), CancellationToken.None);

        await _gateway.Received(1).HoldAsync(
            new PaymentHold(result.Data.Id, "owner-1", 23000m, "COP"),
            Arg.Any<CancellationToken>());
        await _payments.Received(1).AddAsync(
            Arg.Is<WalkPayment>(payment => payment.WalkId == result.Data.Id
                && payment.Status == PaymentStatus.Held
                && payment.GatewayReference == "sim_hold_1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PaymentDeclined_FailsWithoutSavingTheWalk()
    {
        WaggoResponse<string> declined = new();
        declined.AddError(PaymentErrors.Declined);
        _gateway.HoldAsync(Arg.Any<PaymentHold>(), Arg.Any<CancellationToken>()).Returns(declined);

        WaggoResponse<WalkResponse> result = await _sut.HandleAsync(Command(), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(PaymentErrors.Declined.Code);
        await _walks.DidNotReceive().AddAsync(Arg.Any<Walk>(), Arg.Any<CancellationToken>());
        await _payments.DidNotReceive().AddAsync(Arg.Any<WalkPayment>(), Arg.Any<CancellationToken>());
    }
}
