using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ResendLoginCodeRequestValidatorTests
{
    private readonly ResendLoginCodeRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WellFormedRequest_HasNoErrors()
    {
        ValidationResult result = await _validator.ValidateAsync(new ResendLoginCodeRequest(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_MissingChallengeId_IsRequired()
    {
        ValidationResult result = await _validator.ValidateAsync(new ResendLoginCodeRequest(null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }
}
