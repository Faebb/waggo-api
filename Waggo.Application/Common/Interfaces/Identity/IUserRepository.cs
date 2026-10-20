using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Storage of accounts (RF-001).</summary>
public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<User?> GetByConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Persists changes to an entity already tracked from a previous <c>Get</c> call.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
