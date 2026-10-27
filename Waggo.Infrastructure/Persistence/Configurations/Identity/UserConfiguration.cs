using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Infrastructure.Persistence.Configurations.Identity;

/// <summary>Table <c>identity.users</c> (RF-001), alongside <c>identity.walker_profiles</c>.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string Schema = "identity";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users", Schema);
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.EmailConfirmed).IsRequired();
        builder.Property(user => user.IsAdmin).IsRequired();

        builder.Property(user => user.ConfirmationTokenHash).HasMaxLength(64);
        builder.Property(user => user.ConfirmationAttempts).IsRequired();
        builder.Property(user => user.PasswordResetCodeHash).HasMaxLength(64);
        builder.Property(user => user.PasswordResetAttempts).IsRequired();
        builder.Property(user => user.PendingEmail).HasMaxLength(256);
        builder.Property(user => user.EmailChangeCodeHash).HasMaxLength(64);
        builder.Property(user => user.EmailChangeAttempts).IsRequired();

        builder.Property(user => user.RegisteredAt).IsRequired();

        builder.Property(user => user.FullName).HasMaxLength(User.MaxFullNameLength);
        builder.Property(user => user.Phone).HasMaxLength(User.MaxPhoneLength);
    }
}
