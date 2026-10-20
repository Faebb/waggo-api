using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new LoginRequest("ana@mail.com", "Clave123!"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingEmail_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new LoginRequest(null, "Clave123!"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Validate_MissingPassword_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new LoginRequest("ana@mail.com", null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
