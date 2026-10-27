using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class LogoutRequestValidatorTests
{
    private readonly LogoutRequestValidator _validator = new();

    [Fact]
    public async Task Validate_RefreshToken_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new LogoutRequest("some-token"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingRefreshToken_IsRequired(string? refreshToken)
    {
        ValidationResult result = await _validator.ValidateAsync(new LogoutRequest(refreshToken));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
