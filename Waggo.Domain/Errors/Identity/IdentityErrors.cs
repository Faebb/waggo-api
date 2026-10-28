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
        "El código no es válido. Revisa que sean los 6 números del correo.",
        ErrorType.NotFound);

    public static readonly Error ConfirmationTokenExpired = new(
        "Identity.ConfirmationTokenExpired",
        "El código venció, pide uno nuevo.",
        ErrorType.Conflict);

    public static readonly Error ConfirmationAttemptsExceeded = new(
        "Identity.ConfirmationAttemptsExceeded",
        "Demasiados intentos, pide un código nuevo.",
        ErrorType.BusinessRule);

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

    public static readonly Error InvalidFullName = new(
        "Identity.InvalidFullName",
        "El nombre es muy largo.");

    public static readonly Error InvalidPhone = new(
        "Identity.InvalidPhone",
        "El celular es muy largo.");

    public static readonly Error ResetCodeInvalid = new(
        "Identity.ResetCodeInvalid",
        "El código no es válido. Revisa que sean los 6 números del correo.",
        ErrorType.NotFound);

    public static readonly Error ResetCodeExpired = new(
        "Identity.ResetCodeExpired",
        "El código venció, pide uno nuevo.",
        ErrorType.Conflict);

    public static readonly Error ResetAttemptsExceeded = new(
        "Identity.ResetAttemptsExceeded",
        "Demasiados intentos, pide un código nuevo.",
        ErrorType.BusinessRule);

    public static readonly Error CurrentPasswordIncorrect = new(
        "Identity.CurrentPasswordIncorrect",
        "La contraseña actual no es correcta.",
        ErrorType.BusinessRule);

    public static readonly Error PasswordUnchanged = new(
        "Identity.PasswordUnchanged",
        "La contraseña nueva debe ser distinta de la actual.",
        ErrorType.BusinessRule);

    public static readonly Error EmailUnchanged = new(
        "Identity.EmailUnchanged",
        "Ese ya es el correo de tu cuenta.",
        ErrorType.BusinessRule);

    public static readonly Error EmailChangeCodeInvalid = new(
        "Identity.EmailChangeCodeInvalid",
        "El código no es válido. Revisa que sean los 6 números del correo.",
        ErrorType.NotFound);

    public static readonly Error EmailChangeCodeExpired = new(
        "Identity.EmailChangeCodeExpired",
        "El código venció, pide el cambio de correo otra vez.",
        ErrorType.Conflict);

    public static readonly Error EmailChangeAttemptsExceeded = new(
        "Identity.EmailChangeAttemptsExceeded",
        "Demasiados intentos, pide el cambio de correo otra vez.",
        ErrorType.BusinessRule);

    public static readonly Error ExternalTokenInvalid = new(
        "Identity.ExternalTokenInvalid",
        "No pudimos validar tu cuenta de Google o Microsoft, intenta de nuevo.");

    public static readonly Error ExternalEmailNotVerified = new(
        "Identity.ExternalEmailNotVerified",
        "Tu cuenta de Google o Microsoft no tiene el correo verificado. Verifícalo allí e intenta de nuevo.",
        ErrorType.BusinessRule);

    public static readonly Error ExternalProviderUnavailable = new(
        "Identity.ExternalProviderUnavailable",
        "No pudimos conectar con Google o Microsoft en este momento. Intenta de nuevo en unos minutos.",
        ErrorType.ServiceUnavailable);
}
