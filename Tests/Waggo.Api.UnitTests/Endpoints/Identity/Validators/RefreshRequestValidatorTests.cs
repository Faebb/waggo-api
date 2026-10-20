using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new RefreshRequest("some-refresh-token"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingRefreshToken_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new RefreshRequest(null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
