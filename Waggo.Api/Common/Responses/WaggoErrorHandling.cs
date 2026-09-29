using Microsoft.AspNetCore.Diagnostics;

namespace Waggo.Api.Common.Responses;

/// <summary>
/// Wraps the responses the endpoints never see (exceptions, binding errors, unknown routes) in a WaggoApiResponse.
/// </summary>
public static class WaggoErrorHandling
{
    public const string InvalidRequestCode = "Request.Invalid";
    public const string UnexpectedErrorCode = "Server.Unexpected";

    /// <summary>
    /// For <c>UseExceptionHandler</c>. The middleware already logs the exception (with its technical detail)
    /// through Serilog; the client only receives a public message in Spanish.
    /// </summary>
    public static async Task HandleExceptionAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        Exception? exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

        (int status, string code, string message) = exception is BadHttpRequestException bad
            ? (bad.StatusCode, InvalidRequestCode, "La solicitud tiene parámetros inválidos o incompletos.")
            : (StatusCodes.Status500InternalServerError,
                UnexpectedErrorCode,
                "Ocurrió un error inesperado. Intenta de nuevo más tarde.");

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            WaggoApiResponseFactory.FromError(code, message, WaggoApiHttp.TraceId(httpContext)));
    }

    /// <summary>For <c>UseStatusCodePages</c>: 404/405/... without body get the envelope too.</summary>
    public static async Task HandleStatusCodeAsync(StatusCodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        HttpContext httpContext = context.HttpContext;
        int status = httpContext.Response.StatusCode;

        string message = status switch
        {
            StatusCodes.Status404NotFound => "El recurso solicitado no existe.",
            StatusCodes.Status405MethodNotAllowed => "El método HTTP no está permitido para este recurso.",
            StatusCodes.Status401Unauthorized => "Debes iniciar sesión para continuar.",
            StatusCodes.Status403Forbidden => "No tienes permiso para realizar esta acción.",
            _ => $"La solicitud falló con el estado HTTP {status}.",
        };

        await httpContext.Response.WriteAsJsonAsync(
            WaggoApiResponseFactory.FromError($"Http.{status}", message, WaggoApiHttp.TraceId(httpContext)));
    }
}
