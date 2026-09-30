using Waggo.Domain.Common;
using Waggo.Domain.Enums.Pets;
using Waggo.Domain.Errors.Pets;

namespace Waggo.Domain.Entities.Pets;

/// <summary>Dog registered by an owner (RF-004). The medical notes are encrypted at rest (RNF-003).</summary>
public sealed class Pet
{
    public const int MaxNameLength = 50;
    public const int MaxBreedLength = 50;
    public const int MaxMedicalNotesLength = 2000;
    public const int MaxAgeYears = 30;
    public const int MaxPetsPerOwner = 10;
    public const decimal MinWeightKg = 0.5m;
    public const decimal MaxWeightKg = 100m;

    // Used by EF Core to materialize the entity.
    private Pet()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Id of the owner in the identity provider (<c>sub</c> claim).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Breed { get; private set; }

    public PetSize Size { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public decimal? WeightKg { get; private set; }

    public string? MedicalNotes { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Creates a pet, reporting every broken rule at once. Text is trimmed and blank optionals become null.</summary>
    public static WaggoResponse<Pet> Register(
        string ownerId,
        string name,
        string? breed,
        PetSize size,
        DateOnly? birthDate,
        decimal? weightKg,
        string? medicalNotes,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        WaggoResponse<Pet> response = new();

        string trimmedName = name?.Trim() ?? string.Empty;
        string? trimmedBreed = NullIfBlank(breed);
        string? trimmedNotes = NullIfBlank(medicalNotes);
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);

        if (trimmedName.Length is 0 or > MaxNameLength)
        {
            response.AddError(PetErrors.InvalidName);
        }

        if (trimmedBreed?.Length > MaxBreedLength)
        {
            response.AddError(PetErrors.InvalidBreed);
        }

        if (birthDate > today || birthDate < today.AddYears(-MaxAgeYears))
        {
            response.AddError(PetErrors.InvalidBirthDate);
        }

        if (weightKg is < MinWeightKg or > MaxWeightKg)
        {
            response.AddError(PetErrors.InvalidWeight);
        }

        if (trimmedNotes?.Length > MaxMedicalNotesLength)
        {
            response.AddError(PetErrors.InvalidMedicalNotes);
        }

        if (!response.IsValid)
        {
            return response;
        }

        response.Data = new Pet
        {
            Id = Guid.CreateVersion7(now),
            OwnerId = ownerId,
            Name = trimmedName,
            Breed = trimmedBreed,
            Size = size,
            BirthDate = birthDate,
            WeightKg = weightKg,
            MedicalNotes = trimmedNotes,
            RegisteredAt = now,
        };
        return response;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
