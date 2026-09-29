using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Exceptions;

namespace Waggo.Domain.UnitTests.Exceptions;

public class WaggoExceptionTests
{
    private static readonly Error s_error = new("Walks.NotFound", "El paseo no existe.");

    [Fact]
    public void CustomException_CarriesThePublicError_AndItsType()
    {
        NotFoundException exception = new(s_error);

        exception.Error.ShouldBe(s_error);
        exception.ErrorType.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void CustomException_MessageIsTheTechnicalDetail_WhenGiven()
    {
        ConflictException exception = new(s_error, "Row version mismatch");

        exception.Message.ShouldBe("Row version mismatch");
    }

    [Fact]
    public void CustomException_KeepsTheInnerException()
    {
        TimeoutException inner = new();

        ExternalServiceException exception = new(s_error, "Payment gateway timeout", inner);

        exception.InnerException.ShouldBeSameAs(inner);
        exception.ErrorType.ShouldBe(ErrorType.ServiceUnavailable);
    }
}
