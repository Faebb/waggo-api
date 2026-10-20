using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class VerifyLoginCodeRequestValidatorTests
{
    private readonly VerifyLoginCodeRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new VerifyLoginCodeRequest(Guid.NewGuid(), "048213"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingChallengeId_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new VerifyLoginCodeRequest(null, "048213"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Validate_MissingCode_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new VerifyLoginCodeRequest(Guid.NewGuid(), null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
