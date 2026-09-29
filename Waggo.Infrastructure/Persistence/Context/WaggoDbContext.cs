using Microsoft.EntityFrameworkCore;

namespace Waggo.Infrastructure.Persistence.Context;

public sealed class WaggoDbContext(DbContextOptions<WaggoDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Each module adds its IEntityTypeConfiguration<T> classes in this assembly,
        // mapped to its own schema (identity, pets, walks, tracking, payments...).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaggoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
