using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ResendConfirmationRequestValidatorTests
{
    private readonly ResendConfirmationRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedEmail_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new ResendConfirmationRequest("ana@mail.com"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingEmail_IsRequired(string? email)
    {
        ValidationResult result = await _validator.ValidateAsync(new ResendConfirmationRequest(email));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Validate_MalformedEmail_IsInvalid()
    {
        ValidationResult result = await _validator.ValidateAsync(new ResendConfirmationRequest("not-an-email"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }
}
