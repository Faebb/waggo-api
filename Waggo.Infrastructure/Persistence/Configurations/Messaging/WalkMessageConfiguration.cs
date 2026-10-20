using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Messaging;

namespace Waggo.Infrastructure.Persistence.Configurations.Messaging;

/// <summary>Table <c>messaging.walk_messages</c> (RF-013).</summary>
internal sealed class WalkMessageConfiguration : IEntityTypeConfiguration<WalkMessage>
{
    public const string Schema = "messaging";

    public void Configure(EntityTypeBuilder<WalkMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("walk_messages", Schema);
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.SentBy).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(message => message.Text).HasMaxLength(WalkMessage.MaxTextLength).IsRequired();

        // The chat is always read per walk and in time order.
        builder.HasIndex(message => new { message.WalkId, message.SentAt });
    }
}
