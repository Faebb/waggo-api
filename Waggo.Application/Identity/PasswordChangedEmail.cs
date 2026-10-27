namespace Waggo.Application.Identity;

/// <summary>Security notice sent to the account's email after its password changed (RF-005).</summary>
internal static class PasswordChangedEmail
{
    public const string Subject = "Cambió la contraseña de tu cuenta de Waggo";

    public const string Body =
        "La contraseña de tu cuenta de Waggo cambió y cerramos tus sesiones abiertas.\n\n"
        + "Si fuiste tú, no tienes que hacer nada. Si no fuiste tú, restablece tu contraseña desde la pantalla "
        + "de inicio de sesión (\"¿Olvidaste tu contraseña?\") para recuperar el control de tu cuenta.";
}
