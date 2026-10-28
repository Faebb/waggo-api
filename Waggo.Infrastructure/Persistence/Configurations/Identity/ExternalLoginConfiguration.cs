using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Infrastructure.Persistence.Configurations.Identity;

/// <summary>Table <c>identity.external_logins</c>: Google and Microsoft accounts linked to a user (ADR-017).</summary>
internal sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("external_logins", UserConfiguration.Schema);
        builder.HasKey(login => login.Id);
        builder.Property(login => login.Id).ValueGeneratedNever();

        builder.HasOne<User>().WithMany().HasForeignKey(login => login.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(login => login.UserId);

        builder.Property(login => login.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(login => login.ProviderUserId).HasMaxLength(255).IsRequired();
        builder.HasIndex(login => new { login.Provider, login.ProviderUserId }).IsUnique();

        builder.Property(login => login.CreatedAt).IsRequired();
    }
}
