using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Waggo.Infrastructure.Options.Security;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.Persistence.Context;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools to create migrations, without booting the API. It never connects to a
/// database, so the connection string and the encryption key are placeholders.
/// </summary>
internal sealed class WaggoDbContextFactory : IDesignTimeDbContextFactory<WaggoDbContext>
{
    public WaggoDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<WaggoDbContext> options = new DbContextOptionsBuilder<WaggoDbContext>()
            .UseNpgsql("Host=localhost;Database=waggo_design_time")
            .UseSnakeCaseNamingConvention()
            .Options;

        AesGcmFieldEncryptor encryptor = new(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });

        return new WaggoDbContext(options, encryptor);
    }
}
