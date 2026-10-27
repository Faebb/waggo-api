using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class ConfirmRequestValidatorTests
{
    private readonly ConfirmRequestValidator _validator = new();

    [Theory]
    [InlineData("004821")]
    [InlineData("123456")]
    [InlineData("000000")]
    public async Task Validate_SixDigitCode_HasNoErrors(string code)
    {
        ValidationResult result = await _validator.ValidateAsync(new ConfirmRequest("ana@mail.com", code));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingEmail_IsRequired(string? email)
    {
        ValidationResult result = await _validator.ValidateAsync(new ConfirmRequest(email, "123456"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingCode_IsRequired(string? code)
    {
        ValidationResult result = await _validator.ValidateAsync(new ConfirmRequest("ana@mail.com", code));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    [InlineData("12 456")]
    public async Task Validate_CodeThatIsNotSixDigits_IsInvalid(string code)
    {
        ValidationResult result = await _validator.ValidateAsync(new ConfirmRequest("ana@mail.com", code));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }
}
