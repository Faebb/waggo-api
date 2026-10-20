using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Walkers;

public static class WalkerErrors
{
    public static readonly Error InvalidFullName = new(
        "Walkers.InvalidFullName",
        $"Escribe tu nombre completo, de {WalkerProfile.MinFullNameLength} "
        + $"a {WalkerProfile.MaxFullNameLength} caracteres.");

    public static readonly Error InvalidDocument = new(
        "Walkers.InvalidDocument",
        "El número de documento no es válido: usa solo números (de 5 a 15) o, en pasaporte, letras y números.");

    public static readonly Error InvalidPhone = new(
        "Walkers.InvalidPhone",
        "Escribe tu celular de 10 dígitos, sin espacios ni signos.");

    public static readonly Error InvalidExperience = new(
        "Walkers.InvalidExperience",
        $"Cuéntanos tu experiencia en máximo {WalkerProfile.MaxExperienceLength} caracteres.");

    public static readonly Error InvalidRejectionReason = new(
        "Walkers.InvalidRejectionReason",
        "Explica el motivo del rechazo (de 5 a 300 caracteres) para que el paseador sepa qué corregir.");

    public static readonly Error AlreadyRegistered = new(
        "Walkers.AlreadyRegistered",
        "Ya tienes un perfil de paseador.",
        ErrorType.Conflict);

    public static readonly Error NotPending = new(
        "Walkers.NotPending",
        "Este paseador ya fue revisado.",
        ErrorType.BusinessRule);

    public static readonly Error NotVerified = new(
        "Walkers.NotVerified",
        "Tu perfil de paseador aún no está verificado. Te avisaremos cuando puedas recibir paseos.",
        ErrorType.Forbidden);

    public static readonly Error NotFound = new(
        "Walkers.NotFound",
        "No encontramos ese perfil de paseador.",
        ErrorType.NotFound);
}
