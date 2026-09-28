using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints;

internal static class ResultExtensions
{
    /// <summary>Translates a business error into an RFC 9457 ProblemDetails response.</summary>
    public static IResult ToProblem(this Error error) =>
        Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest,
            },
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
