using Waggo.Domain.Entities.Pets;

namespace Waggo.Application.Common.Interfaces.Pets;

/// <summary>Storage of the owners' dogs (RF-004).</summary>
public interface IPetRepository
{
    /// <summary>Saves a new pet right away.</summary>
    Task AddAsync(Pet pet, CancellationToken cancellationToken);

    Task<int> CountByOwnerAsync(string ownerId, CancellationToken cancellationToken);

    /// <summary>Pets of the owner sorted by name.</summary>
    Task<IReadOnlyList<Pet>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken);

    Task<Pet?> GetAsync(Guid id, CancellationToken cancellationToken);
}
