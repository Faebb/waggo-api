using FluentValidation.Results;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Endpoints.Identity.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Identity.Validators;

public class EmailChangeRequestValidatorTests
{
    private readonly RequestEmailChangeRequestValidator _request = new();
    private readonly ConfirmEmailChangeRequestValidator _confirm = new();

    [Fact]
    public async Task Request_NewEmailAndPassword_HasNoErrors()
    {
        ValidationResult result = await _request.ValidateAsync(new RequestEmailChangeRequest("ana@mail.com", "Clave123!"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Request_MissingNewEmail_IsRequired(string? newEmail)
    {
        ValidationResult result = await _request.ValidateAsync(new RequestEmailChangeRequest(newEmail, "Clave123!"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana mail.com")]
    public async Task Request_MalformedNewEmail_IsInvalid(string newEmail)
    {
        ValidationResult result = await _request.ValidateAsync(new RequestEmailChangeRequest(newEmail, "Clave123!"));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Request_MissingPassword_IsRequired(string? password)
    {
        ValidationResult result = await _request.ValidateAsync(new RequestEmailChangeRequest("ana@mail.com", password));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("004821", "session-token")]
    [InlineData("000000", null)]
    public async Task Confirm_SixDigitCode_HasNoErrorsWhateverTheSessionToken(string code, string? refreshToken)
    {
        ValidationResult result = await _confirm.ValidateAsync(new ConfirmEmailChangeRequest(code, refreshToken));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Confirm_MissingCode_IsRequired(string? code)
    {
        ValidationResult result = await _confirm.ValidateAsync(new ConfirmEmailChangeRequest(code, null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.Required");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    public async Task Confirm_CodeThatIsNotSixDigits_IsInvalid(string code)
    {
        ValidationResult result = await _confirm.ValidateAsync(new ConfirmEmailChangeRequest(code, null));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
    }
}
