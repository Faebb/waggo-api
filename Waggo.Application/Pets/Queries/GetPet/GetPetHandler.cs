using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Errors.Pets;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Pets.Queries.GetPet;

/// <summary>RF-004: detail of one of the owner's dogs, with the decrypted medical notes.</summary>
internal sealed class GetPetHandler(IPetRepository pets, ICurrentUser currentUser)
    : IQueryHandler<GetPetQuery, PetResponse>
{
    public async Task<WaggoResponse<PetResponse>> HandleAsync(GetPetQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Pet? pet = await pets.GetAsync(query.Id, cancellationToken);

        // Another owner's pet answers the same as a missing one, so ids of other owners are not revealed.
        if (pet is null || pet.OwnerId != currentUser.Id)
        {
            throw new NotFoundException(PetErrors.NotFound, $"Pet {query.Id} not found for owner {currentUser.Id}");
        }

        return new WaggoResponse<PetResponse> { Data = PetResponse.From(pet) };
    }
}
