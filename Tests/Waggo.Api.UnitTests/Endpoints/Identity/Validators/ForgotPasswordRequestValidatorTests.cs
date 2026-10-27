using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidEmail_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new ForgotPasswordRequest("ana@mail.com"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingEmail_IsRequired(string? email)
    {
        ValidationResult result = await _validator.ValidateAsync(new ForgotPasswordRequest(email));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana mail.com")]
    public async Task Validate_MalformedEmail_IsInvalid(string email)
    {
        ValidationResult result = await _validator.ValidateAsync(new ForgotPasswordRequest(email));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }
}
