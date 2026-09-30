using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Application.Common.Interfaces.Walkers;

/// <summary>Storage of the walkers' profiles (RF-002, RF-003).</summary>
public interface IWalkerProfileRepository
{
    /// <summary>Saves a new profile right away.</summary>
    Task AddAsync(WalkerProfile profile, CancellationToken cancellationToken);

    Task<WalkerProfile?> GetByUserAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Loads a profile to change it; call <see cref="SaveChangesAsync"/> after.</summary>
    Task<WalkerProfile?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Profiles in the given status (all when null), oldest first so nobody waits forever.</summary>
    Task<IReadOnlyList<WalkerProfile>> ListByStatusAsync(
        VerificationStatus? status,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
