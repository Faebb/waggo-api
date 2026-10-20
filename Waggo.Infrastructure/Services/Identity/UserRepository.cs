using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Identity;

internal sealed class UserRepository(WaggoDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetByConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.ConfirmationTokenHash == tokenHash, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
