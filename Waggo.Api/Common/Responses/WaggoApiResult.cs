using System.Diagnostics;
using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Responses;

/// <summary>Turns a WaggoResponse into the HTTP response: WaggoApiResponse body + status code.</summary>
public static class WaggoApiResult
{
    /// <summary>Endpoint without pagination: <c>pagination</c> goes as null.</summary>
    public static IResult ToApiResult<T>(this WaggoResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return Results.Json(
            WaggoApiResponseFactory.From(response, TraceId()),
            statusCode: WaggoApiResponseFactory.StatusCodeFor(response.ErrorType));
    }

    /// <summary>Paged endpoint: <c>data</c> is the page items and <c>pagination</c> is filled.</summary>
    public static IResult ToPagedApiResult<T>(this WaggoResponse<PagedList<T>> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return Results.Json(
            WaggoApiResponseFactory.FromPaged(response, TraceId()),
            statusCode: WaggoApiResponseFactory.StatusCodeFor(response.ErrorType));
    }

    internal static string? TraceId(HttpContext? httpContext = null) =>
        Activity.Current?.TraceId.ToString() ?? httpContext?.TraceIdentifier;
}
