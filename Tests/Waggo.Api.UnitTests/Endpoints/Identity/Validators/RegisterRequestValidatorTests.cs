using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new RegisterRequest("ana@mail.com", "Clave123!"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingEmail_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new RegisterRequest(null, "Clave123!"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("ana@")]
    [InlineData("ana.mail.com")]
    [InlineData("@mail.com")]
    public async Task Validate_MalformedEmail_IsInvalidValue(string email)
    {
        ValidationResult result = await _validator.ValidateAsync(new RegisterRequest(email, "Clave123!"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task Validate_MissingPassword_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new RegisterRequest("ana@mail.com", null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
