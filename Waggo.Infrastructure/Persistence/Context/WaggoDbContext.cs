using Microsoft.EntityFrameworkCore;
using Waggo.Domain.Entities.Pets;
using Waggo.Infrastructure.Persistence.Configurations.Pets;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Context;

public sealed class WaggoDbContext(DbContextOptions<WaggoDbContext> options, AesGcmFieldEncryptor encryptor)
    : DbContext(options)
{
    public DbSet<Pet> Pets => Set<Pet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Each module adds its IEntityTypeConfiguration<T> classes in this assembly, mapped to its own schema
        // (identity, pets, walks, tracking, payments...). Configurations that encrypt columns need the encryptor,
        // so they are applied explicitly; the rest are picked up from the assembly.
        modelBuilder.ApplyConfiguration(new PetConfiguration(encryptor));
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaggoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
