using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Walks;

public static class WalkErrors
{
    public static readonly Error InvalidPets = new(
        "Walks.InvalidPets",
        $"Elige de 1 a {Walk.MaxPets} de tus perros, sin repetir.");

    public static readonly Error InvalidAddress = new(
        "Walks.InvalidAddress",
        $"Escribe la dirección de recogida, de {Walk.MinAddressLength} a {Walk.MaxAddressLength} caracteres.");

    public static readonly Error InvalidLocation = new(
        "Walks.InvalidLocation",
        "No pudimos leer la ubicación de recogida. Actualiza tu ubicación e intenta de nuevo.");

    public static readonly Error InvalidSchedule = new(
        "Walks.InvalidSchedule",
        $"Programa el paseo para ahora o para los próximos {Walk.MaxDaysAhead} días.");

    public static readonly Error InvalidNotes = new(
        "Walks.InvalidNotes",
        $"Las indicaciones pueden tener hasta {Walk.MaxNotesLength} caracteres.");

    public static readonly Error CannotCancel = new(
        "Walks.CannotCancel",
        "Este paseo ya no se puede cancelar.",
        ErrorType.BusinessRule);

    public static readonly Error NotAvailable = new(
        "Walks.NotAvailable",
        "Otro paseador ya tomó este paseo. Busca otra solicitud.",
        ErrorType.Conflict);

    public static readonly Error OwnWalk = new(
        "Walks.OwnWalk",
        "No puedes aceptar un paseo que pediste tú.",
        ErrorType.BusinessRule);

    public static readonly Error NotFound = new(
        "Walks.NotFound",
        "No encontramos ese paseo entre los tuyos.",
        ErrorType.NotFound);
}
