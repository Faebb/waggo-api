using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator = new();

    [Fact]
    public async Task Validate_AllFieldsFilled_HasNoErrors()
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ResetPasswordRequest("ana@mail.com", "004821", "Nueva1234"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingEmail_IsRequired(string? email)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ResetPasswordRequest(email, "004821", "Nueva1234"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingCode_IsRequired(string? code)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ResetPasswordRequest("ana@mail.com", code, "Nueva1234"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    public async Task Validate_CodeThatIsNotSixDigits_IsInvalid(string code)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ResetPasswordRequest("ana@mail.com", code, "Nueva1234"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingNewPassword_IsRequired(string? newPassword)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ResetPasswordRequest("ana@mail.com", "004821", newPassword));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
