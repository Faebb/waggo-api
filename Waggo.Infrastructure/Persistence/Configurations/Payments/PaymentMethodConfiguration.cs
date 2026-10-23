using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Payments;

namespace Waggo.Infrastructure.Persistence.Configurations.Payments;

/// <summary>Table <c>payments.payment_methods</c> (RF-020). No card number, no CVC (RNF-004).</summary>
internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_methods", WalkPaymentConfiguration.Schema);
        builder.HasKey(method => method.Id);
        builder.Property(method => method.Id).ValueGeneratedNever();

        builder.Property(method => method.OwnerId).HasMaxLength(128).IsRequired();
        builder.HasIndex(method => method.OwnerId);

        // At most one default method per owner.
        builder.HasIndex(method => method.OwnerId)
            .IsUnique()
            .HasFilter("is_default")
            .HasDatabaseName("ux_payment_methods_owner_default");

        builder.Property(method => method.Brand).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(method => method.LastFour).HasMaxLength(PaymentMethod.LastFourLength).IsFixedLength().IsRequired();
        builder.Property(method => method.ExpiryMonth).IsRequired();
        builder.Property(method => method.ExpiryYear).IsRequired();
        builder.Property(method => method.IsDefault).IsRequired();
        builder.Property(method => method.RegisteredAt).IsRequired();
    }
}
