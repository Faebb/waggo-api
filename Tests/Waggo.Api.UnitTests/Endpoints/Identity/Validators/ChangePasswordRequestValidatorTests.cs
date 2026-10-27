using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Theory]
    [InlineData("session-token")]
    [InlineData(null)]
    public async Task Validate_BothPasswords_HasNoErrorsWhateverTheSessionToken(string? refreshToken)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ChangePasswordRequest("Actual123", "Nueva1234", refreshToken));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingCurrentPassword_IsRequired(string? current)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ChangePasswordRequest(current, "Nueva1234", "session-token"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingNewPassword_IsRequired(string? newPassword)
    {
        ValidationResult result =
            await _validator.ValidateAsync(new ChangePasswordRequest("Actual123", newPassword, "session-token"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
