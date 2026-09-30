using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Enums.Payments;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Payments;

internal sealed class WalkPaymentRepository(WaggoDbContext db) : IWalkPaymentRepository
{
    public async Task AddAsync(WalkPayment payment, CancellationToken cancellationToken)
    {
        db.WalkPayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<WalkPayment?> GetByWalkAsync(Guid walkId, CancellationToken cancellationToken) =>
        db.WalkPayments.FirstOrDefaultAsync(payment => payment.WalkId == walkId, cancellationToken);

    public async Task<IReadOnlyList<WalkPayment>> ListCapturedByWalkerAsync(
        string walkerId,
        CancellationToken cancellationToken) =>
        await db.WalkPayments.AsNoTracking()
            .Where(payment => payment.WalkerId == walkerId && payment.Status == PaymentStatus.Captured)
            .OrderByDescending(payment => payment.CapturedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
