using Waggo.Domain.Common;

namespace Waggo.Api.Infrastructure.Errors;

/// <summary>Errors of the first validation layer (request DTO shape). Messages tell the user how to fix them.</summary>
public static class RequestErrors
{
    public static Error Required(string field) => new(
        "Request.Required",
        $"El campo '{field}' es obligatorio. Envíalo e intenta de nuevo.");

    public static Error InvalidValue(string field, string howToFix) => new(
        "Request.InvalidValue",
        $"El valor de '{field}' no es válido. {howToFix}");
}
