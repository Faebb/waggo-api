namespace Waggo.Application.Identity.Commands.Login;

/// <summary>The email carrying the second-factor code (RF-005), shared by login and code resend.</summary>
internal static class LoginCodeEmail
{
    public const string Subject = "Tu código de inicio de sesión en Waggo";

    public static string Body(string code) => $"Tu código de verificación es: {code}";
}
