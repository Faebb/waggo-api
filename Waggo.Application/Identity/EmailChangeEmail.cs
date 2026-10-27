using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>The emails of the email change (RF-005): the code for the new inbox and the notice for the old one.</summary>
internal static class EmailChangeEmail
{
    public const string CodeSubject = "Tu código para confirmar el nuevo correo de Waggo";

    public const string NoticeSubject = "Cambió el correo de tu cuenta de Waggo";

    public static string CodeBody(string code)
    {
        int minutes = (int)User.EmailChangeCodeLifetime.TotalMinutes;
        return $"Tu código para confirmar este correo en tu cuenta de Waggo es: {code}\n\n"
            + $"Vence en {minutes} minutos. Si no lo pediste, ignora este correo: no pasa nada.";
    }

    public static string NoticeBody(string newEmail) =>
        $"El correo de tu cuenta de Waggo cambió a {newEmail} y cerramos tus sesiones abiertas en otros dispositivos.\n\n"
        + "Si fuiste tú, no tienes que hacer nada. Si no fuiste tú, escríbenos cuanto antes para recuperar tu cuenta.";
}
