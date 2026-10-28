using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class MicrosoftLoginRequestValidatorTests
{
    private readonly MicrosoftLoginRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithAToken_HasNoErrors()
    {
        ValidationResult result =
            await _validator.ValidateAsync(new MicrosoftLoginRequest("eyJhbGciOi.payload.signature"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_MissingToken_IsRequired(string? idToken)
    {
        ValidationResult result = await _validator.ValidateAsync(new MicrosoftLoginRequest(idToken));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
