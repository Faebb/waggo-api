using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Pets;

namespace Waggo.Application.Pets.Commands.RegisterPet;

public sealed record RegisterPetCommand(
    string Name,
    string? Breed,
    PetSize Size,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? MedicalNotes) : ICommand<PetResponse>;
