using Waggo.Domain.Entities.Pets;

namespace Waggo.Application.Pets;

/// <summary>A dog as the owner sees it. Shared by every Pets use case.</summary>
public sealed record PetResponse(
    Guid Id,
    string Name,
    string? Breed,
    string Size,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? MedicalNotes)
{
    public static PetResponse From(Pet pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        return new PetResponse(
            pet.Id,
            pet.Name,
            pet.Breed,
            pet.Size.ToString(),
            pet.BirthDate,
            pet.WeightKg,
            pet.MedicalNotes);
    }
}
