using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Pets;

public static class PetErrors
{
    public static readonly Error InvalidName = new(
        "Pets.InvalidName",
        $"Escribe el nombre de tu perro, de 1 a {Pet.MaxNameLength} caracteres.");

    public static readonly Error InvalidBreed = new(
        "Pets.InvalidBreed",
        $"La raza puede tener hasta {Pet.MaxBreedLength} caracteres.");

    public static readonly Error InvalidWeight = new(
        "Pets.InvalidWeight",
        $"El peso debe estar entre {Pet.MinWeightKg} y {Pet.MaxWeightKg} kg.");

    public static readonly Error InvalidBirthDate = new(
        "Pets.InvalidBirthDate",
        $"La fecha de nacimiento no puede ser futura ni de hace más de {Pet.MaxAgeYears} años.");

    public static readonly Error InvalidMedicalNotes = new(
        "Pets.InvalidMedicalNotes",
        $"Las notas médicas pueden tener hasta {Pet.MaxMedicalNotesLength} caracteres.");

    public static readonly Error LimitReached = new(
        "Pets.LimitReached",
        $"Ya registraste {Pet.MaxPetsPerOwner} perros, el máximo por cuenta.",
        ErrorType.BusinessRule);

    public static readonly Error NotFound = new(
        "Pets.NotFound",
        "No encontramos ese perro entre los tuyos.",
        ErrorType.NotFound);
}
