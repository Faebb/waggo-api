using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Responses;

/// <summary>Pure mapping WaggoResponse → WaggoApiResponse (+ HTTP status). Filters out Internal messages.</summary>
public static class WaggoApiResponseFactory
{
    public static WaggoApiResponse<T> From<T>(WaggoResponse<T> response, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(response);
        return Build(response, response.ValueOrDefault, pagination: null, traceId);
    }

    public static WaggoApiResponse<IReadOnlyList<T>> FromPaged<T>(WaggoResponse<PagedList<T>> response, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(response);
        var page = response.ValueOrDefault;
        var pagination = page is null
            ? null
            : new WaggoApiPagination(page.Page, page.PageSize, page.TotalItems, page.TotalPages, page.HasPrevious, page.HasNext);

        return Build(response, page?.Items, pagination, traceId);
    }

    /// <summary>Envelope for failures that happen outside a use case (binding errors, unknown routes, exceptions).</summary>
    public static WaggoApiResponse<object> FromError(string code, string message, string? traceId) =>
        new(false, null, null, [new WaggoApiMessage(code, message)], [], [], traceId);

    public static int StatusCodeFor(WaggoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.ErrorType switch
        {
            ErrorType.None => StatusCodes.Status200OK,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest,
        };
    }

    private static WaggoApiResponse<TData> Build<TData>(
        WaggoResponse response, TData? data, WaggoApiPagination? pagination, string? traceId) =>
        new(
            response.IsSuccess,
            response.IsSuccess ? data : default,
            response.IsSuccess ? pagination : null,
            Public(response.Errors),
            Public(response.Warnings),
            Public(response.Infos),
            traceId);

    private static List<WaggoApiMessage> Public(IEnumerable<WaggoMessage> messages) =>
        [.. messages
            .Where(m => m.Visibility == MessageVisibility.Public)
            .Select(m => new WaggoApiMessage(m.Code, m.Message, m.Field))];
}
