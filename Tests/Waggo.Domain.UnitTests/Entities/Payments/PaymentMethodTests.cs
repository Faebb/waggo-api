using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Domain.UnitTests.Entities.Payments;

public class PaymentMethodTests
{
    // Reference date of RF-020: October 2026.
    private static readonly DateTimeOffset s_now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private static WaggoResponse<PaymentMethod> Register(
        string lastFour = "4242",
        int expiryMonth = 12,
        int expiryYear = 2028,
        bool isDefault = true) =>
        PaymentMethod.Register("owner-1", CardBrand.Visa, lastFour, expiryMonth, expiryYear, isDefault, s_now);

    [Fact]
    public void Register_ValidData_CreatesThePaymentMethod()
    {
        WaggoResponse<PaymentMethod> result = Register();

        result.IsValid.ShouldBeTrue();
        PaymentMethod method = result.Data;
        method.Id.ShouldNotBe(Guid.Empty);
        method.OwnerId.ShouldBe("owner-1");
        method.Brand.ShouldBe(CardBrand.Visa);
        method.LastFour.ShouldBe("4242");
        method.ExpiryMonth.ShouldBe(12);
        method.ExpiryYear.ShouldBe(2028);
        method.IsDefault.ShouldBeTrue();
        method.RegisteredAt.ShouldBe(s_now);
    }

    [Fact]
    public void Register_ExpiryInTheCurrentMonth_IsAccepted()
    {
        WaggoResponse<PaymentMethod> result = Register(expiryMonth: 10, expiryYear: 2026);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Register_ExpiryInThePastMonth_ReturnsCardExpired()
    {
        WaggoResponse<PaymentMethod> result = Register(expiryMonth: 9, expiryYear: 2026);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == PaymentErrors.CardExpired.Code);
    }

    [Theory]
    [InlineData("4242424242424242")]
    [InlineData("4242 4242 4242 4242")]
    public void Register_FullCardNumber_ReturnsFullCardNumberNotAllowed(string lastFour)
    {
        WaggoResponse<PaymentMethod> result = Register(lastFour: lastFour);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == PaymentErrors.FullCardNumberNotAllowed.Code);
    }

    [Theory]
    [InlineData("424")]
    [InlineData("42a2")]
    public void Register_LastFourNotFourDigits_ReturnsInvalidLastFour(string lastFour)
    {
        WaggoResponse<PaymentMethod> result = Register(lastFour: lastFour);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == PaymentErrors.InvalidLastFour.Code);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(0)]
    public void Register_MonthOutOfRange_ReturnsInvalidExpiry(int expiryMonth)
    {
        WaggoResponse<PaymentMethod> result = Register(expiryMonth: expiryMonth);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == PaymentErrors.InvalidExpiry.Code);
    }

    [Fact]
    public void Register_YearMoreThanTwentyYearsAhead_ReturnsInvalidExpiry()
    {
        WaggoResponse<PaymentMethod> result = Register(expiryYear: 2047);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == PaymentErrors.InvalidExpiry.Code);
    }
}
