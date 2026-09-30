using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Waggo.Domain.Entities.Tracking;
using Waggo.Infrastructure.Persistence.Converters;

namespace Waggo.Infrastructure.Persistence.Configurations.Tracking;

/// <summary>Table <c>tracking.walk_alerts</c> (RF-012; RF-009 and RF-010 will add other kinds).</summary>
internal sealed class WalkAlertConfiguration : IEntityTypeConfiguration<WalkAlert>
{
    public void Configure(EntityTypeBuilder<WalkAlert> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("walk_alerts", TrackPointConfiguration.Schema);
        builder.HasKey(alert => alert.Id);
        builder.Property(alert => alert.Id).ValueGeneratedNever();

        builder.Property(alert => alert.WalkId).IsRequired();
        builder.HasIndex(alert => alert.WalkId);

        builder.Property(alert => alert.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(alert => alert.RaisedBy).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(alert => alert.Message).HasMaxLength(WalkAlert.MaxMessageLength);
        // The location is optional; EF Core never passes null to a converter, so the non-null one is reused.
        builder.Property(alert => alert.Location)
            .HasConversion((ValueConverter)new GeoPointConverter())
            .HasColumnType("geography (point, 4326)");
        builder.Property(alert => alert.RaisedAt).IsRequired();
    }
}
