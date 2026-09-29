using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Responses;

/// <summary>Maps a WaggoResponse to the public WaggoApiResponse. Internal messages are left out.</summary>
public static class WaggoApiResponseFactory
{
    public static WaggoApiResponse<T> From<T>(WaggoResponse<T> response, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new WaggoApiResponse<T>(
            response.IsValid,
            response.IsValid ? response.Data : default,
            null,
            Public(response.Errors),
            Public(response.Warnings),
            Public(response.Infos),
            traceId);
    }

    public static WaggoApiResponse<IReadOnlyList<T>> FromPaged<T>(WaggoResponse<PagedList<T>> response, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(response);

        PagedList<T>? page = response.IsValid ? response.Data : null;
        WaggoApiPagination? pagination = page is null
            ? null
            : new WaggoApiPagination(
                page.Page, page.PageSize, page.TotalItems, page.TotalPages, page.HasPrevious, page.HasNext);

        return new WaggoApiResponse<IReadOnlyList<T>>(
            response.IsValid,
            page?.Items,
            pagination,
            Public(response.Errors),
            Public(response.Warnings),
            Public(response.Infos),
            traceId);
    }

    /// <summary>
    /// Envelope for failures that happen outside a use case (binding errors, unknown routes, exceptions).
    /// </summary>
    public static WaggoApiResponse<object> FromError(string code, string message, string? traceId) =>
        new(false, null, null, [new WaggoApiMessage(code, message)], [], [], traceId);

    public static int StatusCodeFor(ErrorType errorType) => errorType switch
    {
        ErrorType.None => StatusCodes.Status200OK,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };

    private static List<WaggoApiMessage> Public(List<WaggoMessage> messages) =>
        [.. messages
            .Where(m => m.Visibility == MessageVisibility.Public)
            .Select(m => new WaggoApiMessage(m.Code, m.Message))];
}
