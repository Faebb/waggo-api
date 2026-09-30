using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Common.Interfaces.Walks;

/// <summary>Storage of the walks (RF-007).</summary>
public interface IWalkRepository
{
    /// <summary>Saves a new walk right away.</summary>
    Task AddAsync(Walk walk, CancellationToken cancellationToken);

    /// <summary>Loads a walk to read or change it; call <see cref="SaveChangesAsync"/> after changing it.</summary>
    Task<Walk?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Walks of the owner, newest request first.</summary>
    Task<IReadOnlyList<Walk>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
