using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Identity;

public static class EmailErrors
{
    public static readonly Error SendFailed = new(
        "Email.SendFailed",
        "No pudimos enviar el correo. Intenta de nuevo en unos minutos.",
        ErrorType.ServiceUnavailable);
}
