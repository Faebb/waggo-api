using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Waggo.Domain.Entities.Walkers;
using Waggo.Infrastructure.Persistence.Converters;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Configurations.Walkers;

/// <summary>Table <c>identity.walker_profiles</c> (RF-002, RF-003). The document is encrypted (RNF-003).</summary>
internal sealed class WalkerProfileConfiguration(AesGcmFieldEncryptor encryptor)
    : IEntityTypeConfiguration<WalkerProfile>
{
    public const string Schema = "identity";

    public void Configure(EntityTypeBuilder<WalkerProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("walker_profiles", Schema);
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).HasMaxLength(128).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.FullName).HasMaxLength(WalkerProfile.MaxFullNameLength).IsRequired();
        builder.Property(profile => profile.DocumentType).HasConversion<string>().HasMaxLength(5).IsRequired();

        // The column only ever holds cipher; the converter is shared with the other encrypted columns.
        builder.Property(profile => profile.DocumentNumber)
            .HasConversion((ValueConverter)new EncryptedStringConverter(encryptor))
            .IsRequired();

        builder.Property(profile => profile.Phone).HasMaxLength(15).IsRequired();
        builder.Property(profile => profile.Experience).HasMaxLength(WalkerProfile.MaxExperienceLength);
        builder.Property(profile => profile.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.HasIndex(profile => profile.Status);
        builder.Property(profile => profile.RejectionReason).HasMaxLength(WalkerProfile.MaxRejectionReasonLength);
        builder.Property(profile => profile.ReviewedBy).HasMaxLength(128);
        builder.Ignore(profile => profile.DocumentLast4);
        builder.Ignore(profile => profile.IsVerified);
    }
}
