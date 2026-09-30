using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Common.Interfaces.Payments;

/// <summary>Storage of the walks' payments (RF-015 – RF-018).</summary>
public interface IWalkPaymentRepository
{
    /// <summary>Saves a new payment right away.</summary>
    Task AddAsync(WalkPayment payment, CancellationToken cancellationToken);

    /// <summary>Loads the payment of a walk to change it; call <see cref="SaveChangesAsync"/> after.</summary>
    Task<WalkPayment?> GetByWalkAsync(Guid walkId, CancellationToken cancellationToken);

    /// <summary>The payouts a walker received, the most recent first.</summary>
    Task<IReadOnlyList<WalkPayment>> ListCapturedByWalkerAsync(string walkerId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
