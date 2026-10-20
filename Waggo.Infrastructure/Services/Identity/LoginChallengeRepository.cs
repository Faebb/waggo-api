using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Identity;

internal sealed class LoginChallengeRepository(WaggoDbContext db) : ILoginChallengeRepository
{
    public async Task AddAsync(LoginChallenge challenge, CancellationToken cancellationToken)
    {
        db.LoginChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<LoginChallenge?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.LoginChallenges.FirstOrDefaultAsync(challenge => challenge.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
