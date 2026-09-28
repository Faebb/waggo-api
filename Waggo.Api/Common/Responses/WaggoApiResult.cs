using System.Diagnostics;
using Waggo.Application.Common.Logging;
using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Responses;

/// <summary>
/// IResult that turns a WaggoResponse into a WaggoApiResponse. Before writing the body it logs every message
/// that nobody logged yet (safety net), so no error is ever lost even if the endpoint forgot <c>WriteLogs</c>.
/// </summary>
internal sealed class WaggoApiResult<TData>(WaggoResponse source, Func<string?, WaggoApiResponse<TData>> build) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (source.PendingLogMessages().Any())
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Waggo.Api.Responses");
            source.WriteLogs(logger, httpContext.GetEndpoint()?.DisplayName);
        }

        httpContext.Response.StatusCode = WaggoApiResponseFactory.StatusCodeFor(source);
        await httpContext.Response.WriteAsJsonAsync(build(WaggoApiHttp.TraceId(httpContext)));
    }
}

public static class WaggoApiResultExtensions
{
    /// <summary>Endpoint without pagination: <c>pagination</c> goes as null.</summary>
    public static IResult ToApiResult<T>(this WaggoResponse<T> response) =>
        new WaggoApiResult<T>(response, traceId => WaggoApiResponseFactory.From(response, traceId));

    /// <summary>Paged endpoint: <c>data</c> is the page items and <c>pagination</c> is filled.</summary>
    public static IResult ToPagedApiResult<T>(this WaggoResponse<PagedList<T>> response) =>
        new WaggoApiResult<IReadOnlyList<T>>(response, traceId => WaggoApiResponseFactory.FromPaged(response, traceId));
}

internal static class WaggoApiHttp
{
    public static string TraceId(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
}
