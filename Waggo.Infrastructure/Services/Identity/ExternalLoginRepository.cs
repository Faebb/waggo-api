using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Identity;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Identity;

internal sealed class ExternalLoginRepository(WaggoDbContext db) : IExternalLoginRepository
{
    public Task<ExternalLogin?> GetAsync(
        ExternalProvider provider,
        string providerUserId,
        CancellationToken cancellationToken) =>
        db.ExternalLogins.FirstOrDefaultAsync(
            login => login.Provider == provider && login.ProviderUserId == providerUserId, cancellationToken);

    public async Task AddAsync(ExternalLogin externalLogin, CancellationToken cancellationToken)
    {
        db.ExternalLogins.Add(externalLogin);
        await db.SaveChangesAsync(cancellationToken);
    }
}
