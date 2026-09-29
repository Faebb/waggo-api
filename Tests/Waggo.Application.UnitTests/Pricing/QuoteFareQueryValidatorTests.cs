using FluentValidation.Results;
using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Pricing;

namespace Waggo.Application.UnitTests.Pricing;

public class QuoteFareQueryValidatorTests
{
    private readonly QuoteFareQueryValidator _validator = new();

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(120)]
    public async Task Validate_ValidQuery_HasNoErrors(int minutes)
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareQuery(WalkType.Individual, minutes));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(135)]
    public async Task Validate_InvalidDuration_FailsWithDurationCode(int minutes)
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareQuery(WalkType.Individual, minutes));

        result.Errors.Single().ErrorCode.ShouldBe(PricingErrors.InvalidDuration.Code);
        result.Errors.Single().ErrorMessage.ShouldBe(PricingErrors.InvalidDuration.Message);
    }

    [Fact]
    public async Task Validate_UnknownWalkType_FailsWithWalkTypeCode()
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareQuery((WalkType)99, 60));

        result.Errors.Single().ErrorCode.ShouldBe(PricingErrors.InvalidWalkType.Code);
    }
}
