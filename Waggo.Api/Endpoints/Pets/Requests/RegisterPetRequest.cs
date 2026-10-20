using Waggo.Application.Pets.Commands.RegisterPet;
using Waggo.Domain.Enums.Pets;

namespace Waggo.Api.Endpoints.Pets.Requests;

/// <summary>Body of <c>POST /api/v1/pets</c>. Raw values: they are validated before use.</summary>
public sealed record RegisterPetRequest(
    string? Name,
    string? Breed,
    string? Size,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? MedicalNotes)
{
    /// <summary>Only call it after the request passed <c>RegisterPetRequestValidator</c>.</summary>
    public RegisterPetCommand ToCommand() =>
        new(Name!, Breed, Enum.Parse<PetSize>(Size!, ignoreCase: true), BirthDate, WeightKg, MedicalNotes);
}
