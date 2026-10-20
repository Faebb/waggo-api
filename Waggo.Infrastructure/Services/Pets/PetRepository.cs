using Microsoft.EntityFrameworkCore;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Domain.Entities.Pets;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Infrastructure.Services.Pets;

internal sealed class PetRepository(WaggoDbContext db) : IPetRepository
{
    public async Task AddAsync(Pet pet, CancellationToken cancellationToken)
    {
        db.Pets.Add(pet);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        db.Pets.CountAsync(pet => pet.OwnerId == ownerId, cancellationToken);

    public async Task<IReadOnlyList<Pet>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        await db.Pets.AsNoTracking()
            .Where(pet => pet.OwnerId == ownerId)
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    public Task<Pet?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Pets.AsNoTracking().FirstOrDefaultAsync(pet => pet.Id == id, cancellationToken);
}
