using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Notifications;

namespace Waggo.Infrastructure.Persistence.Configurations.Messaging;

/// <summary>Table <c>messaging.notifications</c> (RF-014), read by user from the newest.</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notifications", "messaging");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();

        builder.Property(notification => notification.UserId).HasMaxLength(128).IsRequired();
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
        builder.Property(notification => notification.RecipientParty).HasConversion<string>().HasMaxLength(10);
        builder.Property(notification => notification.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(notification => notification.Priority).HasConversion<string>().HasMaxLength(10);
        builder.Property(notification => notification.Title).HasMaxLength(100).IsRequired();
        builder.Property(notification => notification.Body).HasMaxLength(300).IsRequired();
    }
}
