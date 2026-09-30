using Microsoft.EntityFrameworkCore;
using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Entities.Walks;
using Waggo.Infrastructure.Persistence.Configurations.Pets;
using Waggo.Infrastructure.Persistence.Configurations.Walkers;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Context;

public sealed class WaggoDbContext(DbContextOptions<WaggoDbContext> options, AesGcmFieldEncryptor encryptor)
    : DbContext(options)
{
    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Walk> Walks => Set<Walk>();

    public DbSet<TrackPoint> TrackPoints => Set<TrackPoint>();

    public DbSet<WalkAlert> WalkAlerts => Set<WalkAlert>();

    public DbSet<WalkMessage> WalkMessages => Set<WalkMessage>();

    public DbSet<WalkerProfile> WalkerProfiles => Set<WalkerProfile>();

    public DbSet<WalkPayment> WalkPayments => Set<WalkPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Each module adds its IEntityTypeConfiguration<T> classes in this assembly, mapped to its own schema
        // (identity, pets, walks, tracking, payments...). Configurations that encrypt columns need the encryptor,
        // so they are applied explicitly; the rest are picked up from the assembly.
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfiguration(new PetConfiguration(encryptor));
        modelBuilder.ApplyConfiguration(new WalkerProfileConfiguration(encryptor));
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaggoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
