using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Storage of the in-flight second factor of a login (RF-005).</summary>
public interface ILoginChallengeRepository
{
    Task AddAsync(LoginChallenge challenge, CancellationToken cancellationToken);

    Task<LoginChallenge?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Persists changes to an entity already tracked from a previous <c>Get</c> call.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
