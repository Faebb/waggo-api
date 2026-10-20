using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Tracking;
using Waggo.Infrastructure.Persistence.Converters;

namespace Waggo.Infrastructure.Persistence.Configurations.Tracking;

/// <summary>
/// Table <c>tracking.track_points</c> (RF-008). GiST on the position for spatial queries and BRIN on the time, as the
/// table only grows in time order (vault: Base de datos - PostgreSQL, RNF-007).
/// </summary>
internal sealed class TrackPointConfiguration : IEntityTypeConfiguration<TrackPoint>
{
    public const string Schema = "tracking";

    public void Configure(EntityTypeBuilder<TrackPoint> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("track_points", Schema);
        builder.HasKey(point => point.Id);
        builder.Property(point => point.Id).ValueGeneratedNever();

        builder.Property(point => point.WalkId).IsRequired();
        builder.HasIndex(point => point.WalkId);

        builder.Property(point => point.Location)
            .HasConversion(new GeoPointConverter())
            .HasColumnType("geography (point, 4326)")
            .IsRequired();
        builder.HasIndex(point => point.Location).HasMethod("gist");

        builder.Property(point => point.RecordedAt).IsRequired();
        builder.HasIndex(point => point.RecordedAt).HasMethod("brin");
    }
}
