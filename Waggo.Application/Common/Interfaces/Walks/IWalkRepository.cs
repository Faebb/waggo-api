using Waggo.Domain.Entities.Walks;
using Waggo.Domain.ValueObjects.Walks;

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

    /// <summary>Open requests (status <c>Requested</c>) of every owner.</summary>
    Task<IReadOnlyList<Walk>> ListRequestedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Open requests whose pickup is within <paramref name="radiusKm"/> of <paramref name="point"/>, nearest first
    /// (RF-006). Resolved by the database with its spatial index.
    /// </summary>
    Task<IReadOnlyList<Walk>> ListRequestedNearAsync(
        GeoPoint point,
        double radiusKm,
        CancellationToken cancellationToken);

    /// <summary>Walks accepted by the walker, next one first.</summary>
    Task<IReadOnlyList<Walk>> ListByWalkerAsync(string walkerId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the changes. If another request changed the same walk in the meantime (two walkers accepting at once),
    /// it throws a <c>ConflictException</c> with <c>Walks.NotAvailable</c>.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
