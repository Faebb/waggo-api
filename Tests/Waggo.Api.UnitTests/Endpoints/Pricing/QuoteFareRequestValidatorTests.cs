using FluentValidation.Results;
using Waggo.Api.Endpoints.Pricing;

namespace Waggo.Api.UnitTests.Endpoints.Pricing;

public class QuoteFareRequestValidatorTests
{
    private readonly QuoteFareRequestValidator _validator = new();

    [Theory]
    [InlineData("Individual", 60)]
    [InlineData("group", 45)]
    [InlineData("Individual", 20)] // shape is fine; 20 minutes is rejected later by the business validator
    public async Task Validate_WellFormedRequest_HasNoErrors(string walkType, int minutes)
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareRequest(walkType, minutes));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingWalkType_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareRequest(null, 60));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
        result.Errors.Single().ErrorMessage.ShouldContain("walkType");
    }

    [Theory]
    [InlineData("Skateboard")]
    [InlineData("1")]
    public async Task Validate_UnknownWalkType_IsInvalidValue_AndListsTheAllowedValues(string walkType)
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareRequest(walkType, 60));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
        result.Errors.Single().ErrorMessage.ShouldContain("Individual, Group");
    }

    [Fact]
    public async Task Validate_MissingDuration_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareRequest("Individual", null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Validate_NonPositiveDuration_IsInvalidValue()
    {
        ValidationResult result = await _validator.ValidateAsync(new QuoteFareRequest("Individual", 0));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }
}
