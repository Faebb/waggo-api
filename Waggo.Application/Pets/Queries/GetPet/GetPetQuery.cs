using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Pets.Queries.GetPet;

public sealed record GetPetQuery(Guid Id) : IQuery<PetResponse>;
