using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Infrastructure.Persistence.Configurations.Identity;

/// <summary>Table <c>identity.refresh_tokens</c> (RNF-002, rotating sessions).</summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_tokens", UserConfiguration.Schema);
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.UserId).IsRequired();
        builder.HasIndex(token => token.UserId);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.Property(token => token.ExpiresAt).IsRequired();
    }
}
