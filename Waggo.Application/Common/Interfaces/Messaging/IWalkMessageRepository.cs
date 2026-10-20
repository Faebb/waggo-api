using Waggo.Domain.Entities.Messaging;

namespace Waggo.Application.Common.Interfaces.Messaging;

/// <summary>Storage of the chat messages of the walks (RF-013).</summary>
public interface IWalkMessageRepository
{
    /// <summary>Saves a new message right away.</summary>
    Task AddAsync(WalkMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<WalkMessage>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken);
}
