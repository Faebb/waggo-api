using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Identity;

public static class IdentityErrors
{
    public static readonly Error InvalidEmail = new(
        "Identity.InvalidEmail",
        "Escribe un correo válido.");

    public static readonly Error WeakPassword = new(
        "Identity.WeakPassword",
        "La contraseña debe tener al menos 8 caracteres, una mayúscula y un número.");

    public static readonly Error EmailAlreadyRegistered = new(
        "Identity.EmailAlreadyRegistered",
        "Ese correo ya tiene una cuenta. ¿Olvidaste tu contraseña?",
        ErrorType.BusinessRule);

    public static readonly Error ConfirmationTokenInvalid = new(
        "Identity.ConfirmationTokenInvalid",
        "El enlace de confirmación no es válido.",
        ErrorType.NotFound);

    public static readonly Error ConfirmationTokenExpired = new(
        "Identity.ConfirmationTokenExpired",
        "El enlace de confirmación expiró, pide uno nuevo.",
        ErrorType.Conflict);

    public static readonly Error InvalidCredentials = new(
        "Identity.InvalidCredentials",
        "Correo o contraseña incorrectos.");

    public static readonly Error AccountNotConfirmed = new(
        "Identity.AccountNotConfirmed",
        "Confirma tu correo antes de iniciar sesión.",
        ErrorType.BusinessRule);

    public static readonly Error TooManyAttempts = new(
        "Identity.TooManyAttempts",
        "Demasiados intentos, espera 15 minutos.",
        ErrorType.BusinessRule);

    public static readonly Error RefreshTokenInvalid = new(
        "Identity.RefreshTokenInvalid",
        "Tu sesión expiró, inicia sesión de nuevo.",
        ErrorType.Unauthorized);
}
