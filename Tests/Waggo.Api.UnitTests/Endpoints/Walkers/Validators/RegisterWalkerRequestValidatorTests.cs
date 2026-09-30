using FluentValidation.Results;
using Waggo.Api.Endpoints.Walkers.Requests;
using Waggo.Api.Endpoints.Walkers.Validators;

namespace Waggo.Api.UnitTests.Endpoints.Walkers.Validators;

public class RegisterWalkerRequestValidatorTests
{
    private readonly RegisterWalkerRequestValidator _validator = new();

    private static RegisterWalkerRequest Request(
        string? fullName = "Andrés Gómez",
        string? documentType = "CC",
        string? documentNumber = "1020304050",
        string? phone = "3001234567") =>
        new(fullName, documentType, documentNumber, phone, null);

    [Theory]
    [InlineData("CC")]
    [InlineData("pp")]
    public async Task Validate_WellFormedRequest_HasNoErrors(string documentType)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(documentType: documentType));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_EmptyRequest_RequiresEveryField()
    {
        ValidationResult result = await _validator.ValidateAsync(new RegisterWalkerRequest(null, null, null, null, null));

        result.Errors.Count.ShouldBe(4);
        result.Errors.ShouldAllBe(error => error.ErrorCode == "Request.Required");
    }

    [Theory]
    [InlineData("NIT")]
    [InlineData("1")]
    public async Task Validate_UnknownDocumentType_IsInvalidValue_AndListsTheAllowedValues(string documentType)
    {
        ValidationResult result = await _validator.ValidateAsync(Request(documentType: documentType));

        result.Errors.Single().ErrorCode.ShouldBe("Request.InvalidValue");
        result.Errors.Single().ErrorMessage.ShouldContain("CC, CE, PP");
    }
}
