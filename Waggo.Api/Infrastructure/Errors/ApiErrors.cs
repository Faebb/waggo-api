using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Api.Infrastructure.Errors;

/// <summary>
/// Generic errors produced by the API itself. Messages tell the user how to solve the problem and never
/// include technical details (exception type, stack trace, internal messages).
/// </summary>
public static class ApiErrors
{
    public static readonly Error InvalidRequest = new(
        "Request.Invalid",
        "La solicitud no tiene el formato esperado. Revisa los datos enviados e intenta de nuevo.");

    public static readonly Error Unauthorized = new(
        "Auth.Unauthorized",
        "Tu sesión no es válida o expiró. Inicia sesión nuevamente.",
        ErrorType.Unauthorized);

    public static readonly Error Forbidden = new(
        "Auth.Forbidden",
        "No tienes permisos para realizar esta acción. Si crees que es un error, comunícate con el administrador.",
        ErrorType.Forbidden);

    public static readonly Error NotFound = new(
        "Http.NotFound",
        "El recurso solicitado no existe. Verifica la dirección e intenta de nuevo.",
        ErrorType.NotFound);

    public static readonly Error MethodNotAllowed = new(
        "Http.MethodNotAllowed",
        "La operación no está permitida para este recurso. Verifica el método de la solicitud.");

    public static readonly Error TooManyRequests = new(
        "Http.TooManyRequests",
        "Hiciste demasiadas solicitudes en poco tiempo. Espera un momento e intenta de nuevo.");

    /// <summary>Unknown failure: the user is asked to contact the administrator with the trace code.</summary>
    public static Error Unexpected(string? traceId) => new(
        "Server.Unexpected",
        "Ocurrió un error inesperado. Intenta de nuevo más tarde y, si el problema continúa, comunícate con el "
        + $"administrador indicando el código de seguimiento {traceId}.",
        ErrorType.Unexpected);

    /// <summary>Any other status without body.</summary>
    public static Error ForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => Unauthorized,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status429TooManyRequests => TooManyRequests,
        _ => new Error(
            $"Http.{status}",
            "No fue posible procesar la solicitud. Intenta de nuevo y, si el problema continúa, comunícate con el "
            + "administrador."),
    };
}
