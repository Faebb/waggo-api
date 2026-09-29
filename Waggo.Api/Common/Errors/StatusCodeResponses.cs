using Microsoft.AspNetCore.Diagnostics;
using Waggo.Api.Common.Responses;
using Waggo.Domain.Common;

namespace Waggo.Api.Common.Errors;

/// <summary>
/// For <c>UseStatusCodePages</c>: 401, 403, 404, 405, 429... without body also get a WaggoApiResponse.
/// </summary>
internal static class StatusCodeResponses
{
    public static async Task WriteAsync(StatusCodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        HttpContext httpContext = context.HttpContext;
        Error error = ApiErrors.ForStatus(httpContext.Response.StatusCode);

        await httpContext.Response.WriteAsJsonAsync(
            WaggoApiResponseFactory.FromError(error.Code, error.Message, WaggoApiResult.TraceId(httpContext)));
    }
}
