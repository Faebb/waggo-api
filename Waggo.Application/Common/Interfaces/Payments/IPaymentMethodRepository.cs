using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Common.Interfaces.Payments;

/// <summary>Storage of the owners' simulated payment methods (RF-020).</summary>
public interface IPaymentMethodRepository
{
    /// <summary>
    /// Saves a new method right away. When it is the default, the owner's previous default stops being default
    /// in the same transaction.
    /// </summary>
    Task AddAsync(PaymentMethod method, CancellationToken cancellationToken);

    /// <summary>Methods of the owner: the default first, then the most recent.</summary>
    Task<IReadOnlyList<PaymentMethod>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken);
}
