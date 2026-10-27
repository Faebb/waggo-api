using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>The email with the 6-digit code to set a new password (RF-001, RF-005).</summary>
internal static class PasswordResetEmail
{
    public const string Subject = "Tu código para restablecer la contraseña de Waggo";

    public static string Body(string code)
    {
        int minutes = (int)User.PasswordResetCodeLifetime.TotalMinutes;
        return $"Tu código para restablecer la contraseña de Waggo es: {code}\n\n"
            + $"Vence en {minutes} minutos. Si no pediste restablecerla, ignora este correo: tu contraseña no cambia.";
    }
}
