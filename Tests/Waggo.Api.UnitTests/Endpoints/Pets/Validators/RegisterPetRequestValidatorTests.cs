using FluentValidation.Results;
using Waggo.Api.Endpoints.Pets.Requests;
using Waggo.Api.Endpoints.Pets.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Pets.Validators;

public class RegisterPetRequestValidatorTests
{
    private readonly RegisterPetRequestValidator _validator = new();

    private static RegisterPetRequest Request(string? name = "Luna", string? size = "Medium") =>
        new(name, "Criolla", size, new DateOnly(2021, 5, 10), 14.5m, "Alérgica al pollo");

    [Theory]
    [InlineData("Medium")]
    [InlineData("small")]
    public async Task Validate_WellFormedRequest_HasNoErrors(string size)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(size: size));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingName_IsRequired(string? name)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(name: name));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
        result.Errors.Single().ErrorMessage.ShouldContain("name");
    }

    [Fact]
    public async Task Validate_MissingSize_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(Request(size: null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("Giant")]
    [InlineData("2")]
    public async Task Validate_UnknownSize_IsInvalidValue_AndListsTheAllowedValues(string size)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(size: size));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
        result.Errors.Single().ErrorMessage.ShouldContain("Small, Medium, Large");
    }
}
