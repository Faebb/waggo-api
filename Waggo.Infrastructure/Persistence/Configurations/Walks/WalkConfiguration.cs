using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Walks;
using Waggo.Infrastructure.Persistence.Converters;

namespace Waggo.Infrastructure.Persistence.Configurations.Walks;

/// <summary>Table <c>walks.walks</c> (RF-007). The pickup point is a PostGIS geography, ready for RF-006.</summary>
internal sealed class WalkConfiguration : IEntityTypeConfiguration<Walk>
{
    public const string Schema = "walks";

    public void Configure(EntityTypeBuilder<Walk> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("walks", Schema);
        builder.HasKey(walk => walk.Id);
        builder.Property(walk => walk.Id).ValueGeneratedNever();

        builder.Property(walk => walk.OwnerId).HasMaxLength(128).IsRequired();
        builder.HasIndex(walk => walk.OwnerId);

        // The pets of a walk are few (max 3) and always read together: a uuid[] column is enough.
        builder.Ignore(walk => walk.PetIds);
        builder.Property<List<Guid>>("_petIds").HasColumnName("pet_ids").IsRequired();

        builder.Property(walk => walk.WalkType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(walk => walk.PickupAddress).HasMaxLength(Walk.MaxAddressLength).IsRequired();
        builder.Property(walk => walk.PickupLocation)
            .HasConversion(new GeoPointConverter())
            .HasColumnType("geography (point, 4326)")
            .IsRequired();
        builder.HasIndex(walk => walk.PickupLocation).HasMethod("gist");

        builder.Property(walk => walk.Notes).HasMaxLength(Walk.MaxNotesLength);
        builder.Property(walk => walk.Currency).HasMaxLength(3).IsRequired();
        builder.Property(walk => walk.Total).HasPrecision(12, 2);
        builder.Property(walk => walk.Commission).HasPrecision(12, 2);
        builder.Property(walk => walk.WalkerPayout).HasPrecision(12, 2);

        builder.Property(walk => walk.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(walk => walk.Status);
        builder.Property(walk => walk.WalkerId).HasMaxLength(128);
    }
}
