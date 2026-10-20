using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Infrastructure.Persistence.Configurations.Payments;

/// <summary>Table <c>payments.walk_payments</c> (RF-015 – RF-018). No card data (RNF-004).</summary>
internal sealed class WalkPaymentConfiguration : IEntityTypeConfiguration<WalkPayment>
{
    public const string Schema = "payments";

    public void Configure(EntityTypeBuilder<WalkPayment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("walk_payments", Schema);
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.HasOne<Walk>().WithOne().HasForeignKey<WalkPayment>(payment => payment.WalkId);
        builder.HasIndex(payment => payment.WalkId).IsUnique();

        builder.Property(payment => payment.OwnerId).HasMaxLength(128).IsRequired();
        builder.Property(payment => payment.WalkerId).HasMaxLength(128);
        builder.HasIndex(payment => new { payment.WalkerId, payment.Status });

        builder.Property(payment => payment.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(payment => payment.Total).HasPrecision(12, 2);
        builder.Property(payment => payment.Commission).HasPrecision(12, 2);
        builder.Property(payment => payment.WalkerPayout).HasPrecision(12, 2);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(payment => payment.GatewayReference).HasMaxLength(128).IsRequired();

        // xmin concurrency, like walks: a capture and a release of the same payment cannot both win.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
