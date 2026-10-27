using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>The confirmation email of RF-001, shared by registration and resend.</summary>
internal static class ConfirmationEmail
{
    public const string Subject = "Tu código de confirmación de Waggo";

    public static string Body(string code)
    {
        int minutes = (int)User.ConfirmationTokenLifetime.TotalMinutes;
        return $"Tu código de confirmación de Waggo es: {code}\n\n"
            + $"Vence en {minutes} minutos. Si no creaste una cuenta en Waggo, ignora este correo.";
    }
}
