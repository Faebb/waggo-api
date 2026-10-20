using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Errors.Pets;

namespace Waggo.Application.Pets.Commands.RegisterPet;

/// <summary>RF-004: the current owner registers a dog.</summary>
internal sealed class RegisterPetHandler(
    IValidator<RegisterPetCommand> validator,
    IPetRepository pets,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterPetCommand, PetResponse>
{
    public async Task<WaggoResponse<PetResponse>> HandleAsync(
        RegisterPetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<PetResponse> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        if (await pets.CountByOwnerAsync(currentUser.Id, cancellationToken) >= Pet.MaxPetsPerOwner)
        {
            response.AddError(PetErrors.LimitReached);
            return response;
        }

        WaggoResponse<Pet> pet = Pet.Register(
            currentUser.Id,
            command.Name,
            command.Breed,
            command.Size,
            command.BirthDate,
            command.WeightKg,
            command.MedicalNotes,
            timeProvider.GetUtcNow());
        response.ConcatStacks(pet);
        if (!response.IsValid)
        {
            return response;
        }

        await pets.AddAsync(pet.Data, cancellationToken);
        response.Data = PetResponse.From(pet.Data);
        return response;
    }
}
