using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Pets.Queries.ListMyPets;

/// <summary>Pets of the current owner. Not paged: an owner has at most ten.</summary>
public sealed record ListMyPetsQuery : IQuery<IReadOnlyList<PetResponse>>;
