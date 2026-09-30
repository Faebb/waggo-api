using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Messaging;
using Waggo.Domain.Entities.Messaging;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Messaging;

internal sealed class WalkMessageRepository(WaggoDbContext db) : IWalkMessageRepository
{
    public async Task AddAsync(WalkMessage message, CancellationToken cancellationToken)
    {
        db.WalkMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WalkMessage>> ListByWalkAsync(Guid walkId, CancellationToken cancellationToken) =>
        await db.WalkMessages.AsNoTracking()
            .Where(message => message.WalkId == walkId)
            .OrderBy(message => message.SentAt)
            .ToListAsync(cancellationToken);
}
