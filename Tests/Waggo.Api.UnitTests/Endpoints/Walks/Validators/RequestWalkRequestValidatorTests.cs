using FluentValidation.Results;
using Waggo.Api.Endpoints.Walks.Requests;
using Waggo.Api.Endpoints.Walks.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Walks.Validators;

public class RequestWalkRequestValidatorTests
{
    private readonly RequestWalkRequestValidator _validator = new();

    private static RequestWalkRequest Request(
        List<Guid>? petIds = null,
        string? walkType = "Individual",
        int? durationMinutes = 60,
        string? pickupAddress = "Cra 7 # 45-10, Bogotá",
        double? latitude = 4.6361,
        double? longitude = -74.0645) =>
        new(petIds ?? [Guid.NewGuid()], walkType, durationMinutes, pickupAddress, latitude, longitude, null, null);

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors() =>
        (await _validator.ValidateAsync(Request())).IsValid.ShouldBeTrue();

    [Fact]
    public async Task Validate_WithoutPets_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(Request(petIds: []));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
        result.Errors.Single().ErrorMessage.ShouldContain("petIds");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_WithoutAddress_IsRequired(string? address)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(pickupAddress: address));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Validate_WithoutCoordinates_AreRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(Request(latitude: null, longitude: null));

        result.Errors.Select(e => e.ErrorCode).ShouldBe(["Request.Required", "Request.Required"]);
    }

    [Fact]
    public async Task Validate_UnknownWalkType_IsInvalidValue()
    {
        ValidationResult result = await _validator.ValidateAsync(Request(walkType: "Skateboard"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task Validate_WithoutDuration_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(Request(durationMinutes: null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
