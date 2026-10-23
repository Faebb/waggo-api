using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Domain.Entities.Payments;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Payments;

internal sealed class PaymentMethodRepository(WaggoDbContext db) : IPaymentMethodRepository
{
    public async Task AddAsync(PaymentMethod method, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (method.IsDefault)
        {
            await db.PaymentMethods
                .Where(existing => existing.OwnerId == method.OwnerId && existing.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(existing => existing.IsDefault, false), cancellationToken);
        }

        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethod>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        await db.PaymentMethods.AsNoTracking()
            .Where(method => method.OwnerId == ownerId)
            .OrderByDescending(method => method.IsDefault)
            .ThenByDescending(method => method.RegisteredAt)
            .ToListAsync(cancellationToken);
}
