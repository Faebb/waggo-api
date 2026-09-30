using Waggo.Domain.Entities.Tracking;

namespace Waggo.Application.Common.Interfaces.Tracking;

/// <summary>Storage of the alerts raised during the walks (RF-012).</summary>
public interface IWalkAlertRepository
{
    /// <summary>Saves a new alert right away.</summary>
    Task AddAsync(WalkAlert alert, CancellationToken cancellationToken);

    Task<IReadOnlyList<WalkAlert>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken);
}
