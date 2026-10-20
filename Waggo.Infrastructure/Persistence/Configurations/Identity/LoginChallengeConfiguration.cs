using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Infrastructure.Persistence.Configurations.Identity;

/// <summary>Table <c>identity.login_challenges</c> (RF-005, second factor).</summary>
internal sealed class LoginChallengeConfiguration : IEntityTypeConfiguration<LoginChallenge>
{
    public void Configure(EntityTypeBuilder<LoginChallenge> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("login_challenges", UserConfiguration.Schema);
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).ValueGeneratedNever();

        builder.Property(challenge => challenge.UserId).IsRequired();
        builder.HasIndex(challenge => challenge.UserId);

        builder.Property(challenge => challenge.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(challenge => challenge.ExpiresAt).IsRequired();
        builder.Property(challenge => challenge.Attempts).IsRequired();
        builder.Property(challenge => challenge.Used).IsRequired();
    }
}
