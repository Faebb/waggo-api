using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Pets;
using Waggo.Infrastructure.Persistence.Converters;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Configurations.Pets;

/// <summary>Table <c>pets.pets</c> (RF-004). The medical notes column is encrypted (RNF-003).</summary>
internal sealed class PetConfiguration(AesGcmFieldEncryptor encryptor) : IEntityTypeConfiguration<Pet>
{
    public const string Schema = "pets";

    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("pets", Schema);
        builder.HasKey(pet => pet.Id);
        builder.Property(pet => pet.Id).ValueGeneratedNever();

        builder.Property(pet => pet.OwnerId).HasMaxLength(128).IsRequired();
        builder.HasIndex(pet => pet.OwnerId);

        builder.Property(pet => pet.Name).HasMaxLength(Pet.MaxNameLength).IsRequired();
        builder.Property(pet => pet.Breed).HasMaxLength(Pet.MaxBreedLength);
        builder.Property(pet => pet.Size).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(pet => pet.WeightKg).HasPrecision(5, 2);

        // Cipher is longer than the text (prefix + nonce + tag + base64), so the column has no length limit.
        builder.Property(pet => pet.MedicalNotes).HasConversion(new EncryptedStringConverter(encryptor));

        builder.Property(pet => pet.RegisteredAt).IsRequired();
    }
}
