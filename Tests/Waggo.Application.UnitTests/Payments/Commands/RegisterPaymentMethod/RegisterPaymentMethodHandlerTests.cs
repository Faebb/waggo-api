using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Payments;
using Waggo.Application.Payments.Commands.RegisterPaymentMethod;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Application.UnitTests.Payments.Commands.RegisterPaymentMethod;

public class RegisterPaymentMethodHandlerTests
{
    private readonly IPaymentMethodRepository _methods = Substitute.For<IPaymentMethodRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RegisterPaymentMethodHandler _sut;

    public RegisterPaymentMethodHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _methods.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns([]);
        _sut = new RegisterPaymentMethodHandler(
            new RegisterPaymentMethodCommandValidator(),
            _methods,
            _currentUser,
            new FixedTimeProvider());
    }

    private static RegisterPaymentMethodCommand Visa(bool isDefault = false, string lastFour = "4242") =>
        new(CardBrand.Visa, lastFour, 12, 2028, isDefault);

    [Fact]
    public async Task HandleAsync_FirstMethod_BecomesTheDefaultEvenWithoutAskingForIt()
    {
        WaggoResponse<PaymentMethodResponse> result = await _sut.HandleAsync(Visa(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.IsDefault.ShouldBeTrue();
        result.Data.Brand.ShouldBe("Visa");
        result.Data.LastFour.ShouldBe("4242");
        await _methods.Received(1).AddAsync(
            Arg.Is<PaymentMethod>(method => method.OwnerId == "owner-1" && method.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SecondMethodWithoutDefault_StaysNotDefault()
    {
        _methods.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns([ExistingVisa()]);

        WaggoResponse<PaymentMethodResponse> result = await _sut.HandleAsync(Visa(), CancellationToken.None);

        result.Data.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_SecondMethodAsDefault_IsDefault()
    {
        _methods.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns([ExistingVisa()]);

        WaggoResponse<PaymentMethodResponse> result = await _sut.HandleAsync(Visa(isDefault: true), CancellationToken.None);

        result.Data.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_FullCardNumber_FailsWithoutSaving()
    {
        WaggoResponse<PaymentMethodResponse> result =
            await _sut.HandleAsync(Visa(lastFour: "4242424242424242"), CancellationToken.None);

        result.Errors.ShouldContain(e => e.Code == PaymentErrors.FullCardNumberNotAllowed.Code);
        result.ErrorType.ShouldBe(ErrorType.Validation);
        await _methods.DidNotReceive().AddAsync(Arg.Any<PaymentMethod>(), Arg.Any<CancellationToken>());
    }

    private static PaymentMethod ExistingVisa() =>
        PaymentMethod.Register("owner-1", CardBrand.Visa, "1111", 12, 2028, true, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)).Data;
}
