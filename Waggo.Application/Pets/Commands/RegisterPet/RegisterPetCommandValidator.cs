using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Errors.Pets;

namespace Waggo.Application.Pets.Commands.RegisterPet;

/// <summary>
/// Field rules of <see cref="RegisterPetCommand"/> (RF-004). Date rules need the clock, so the <see cref="Pet"/>
/// entity checks them when it is created.
/// </summary>
internal sealed class RegisterPetCommandValidator : AbstractValidator<RegisterPetCommand>
{
    public RegisterPetCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(name => name?.Trim().Length is > 0 and <= Pet.MaxNameLength)
            .WithError(PetErrors.InvalidName);

        RuleFor(command => command.Breed)
            .Must(breed => breed is null || breed.Trim().Length <= Pet.MaxBreedLength)
            .WithError(PetErrors.InvalidBreed);

        RuleFor(command => command.WeightKg)
            .Must(weight => weight is null or (>= Pet.MinWeightKg and <= Pet.MaxWeightKg))
            .WithError(PetErrors.InvalidWeight);

        RuleFor(command => command.MedicalNotes)
            .Must(notes => notes is null || notes.Trim().Length <= Pet.MaxMedicalNotesLength)
            .WithError(PetErrors.InvalidMedicalNotes);
    }
}
