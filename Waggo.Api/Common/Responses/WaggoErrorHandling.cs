using Microsoft.AspNetCore.Diagnostics;

namespace Waggo.Api.Common.Responses;

/// <summary>Wraps the responses the endpoints never see (exceptions, binding errors, unknown routes) in a WaggoApiResponse.</summary>
public static class WaggoErrorHandling
{
    public const string InvalidRequestCode = "Request.Invalid";
    public const string UnexpectedErrorCode = "Server.Unexpected";

    /// <summary>For <c>UseExceptionHandler</c>. The middleware already logs the exception through Serilog.</summary>
    public static async Task HandleExceptionAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (status, code, message) = exception is BadHttpRequestException bad
            ? (bad.StatusCode, InvalidRequestCode, bad.Message)
            : (StatusCodes.Status500InternalServerError, UnexpectedErrorCode, "An unexpected error occurred.");

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            WaggoApiResponseFactory.FromError(code, message, WaggoApiHttp.TraceId(httpContext)));
    }

    /// <summary>For <c>UseStatusCodePages</c>: 404/405/... without body get the envelope too.</summary>
    public static async Task HandleStatusCodeAsync(StatusCodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var httpContext = context.HttpContext;
        var status = httpContext.Response.StatusCode;

        await httpContext.Response.WriteAsJsonAsync(
            WaggoApiResponseFactory.FromError($"Http.{status}", $"The request failed with HTTP status {status}.", WaggoApiHttp.TraceId(httpContext)));
    }
}
