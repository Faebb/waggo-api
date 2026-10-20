using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;

namespace Waggo.Application.Pets.Queries.ListMyPets;

/// <summary>RF-004: the owner sees their own dogs, never another owner's.</summary>
internal sealed class ListMyPetsHandler(IPetRepository pets, ICurrentUser currentUser)
    : IQueryHandler<ListMyPetsQuery, IReadOnlyList<PetResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<PetResponse>>> HandleAsync(
        ListMyPetsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Pet> mine = await pets.ListByOwnerAsync(currentUser.Id, cancellationToken);
        return new WaggoResponse<IReadOnlyList<PetResponse>> { Data = [.. mine.Select(PetResponse.From)] };
    }
}
