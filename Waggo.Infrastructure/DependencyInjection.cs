using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Waggo.Application.Common.Interfaces.Messaging;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Common.Interfaces.Pricing;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Infrastructure.Options.Payments;
using Waggo.Infrastructure.Options.Pricing;
using Waggo.Infrastructure.Options.Security;
using Waggo.Infrastructure.Persistence.Context;
using Waggo.Infrastructure.Services.Messaging;
using Waggo.Infrastructure.Services.Payments;
using Waggo.Infrastructure.Services.Pets;
using Waggo.Infrastructure.Services.Pricing;
using Waggo.Infrastructure.Services.Security;
using Waggo.Infrastructure.Services.Tracking;
using Waggo.Infrastructure.Services.Walkers;
using Waggo.Infrastructure.Services.Walks;

namespace Waggo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Waggo")
            ?? throw new InvalidOperationException("Connection string 'Waggo' is not configured.");

        // snake_case tables and columns, one schema per module, PostGIS through NetTopologySuite
        // (vault: Base de datos - PostgreSQL).
        services.AddDbContext<WaggoDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention());

        services.AddOptions<PricingOptions>()
            .Bind(configuration.GetSection(PricingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // RNF-003: key for the encrypted columns. The encryptor rejects a key that is not 32 bytes of base64.
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(provider =>
            new AesGcmFieldEncryptor(provider.GetRequiredService<IOptions<EncryptionOptions>>().Value));

        // RF-015 – RF-018: only the simulated gateway exists until the provider is chosen (see PaymentOptions).
        services.AddOptions<PaymentOptions>()
            .Bind(configuration.GetSection(PaymentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();

        services.AddSingleton<IPricingTableProvider, ConfigurationPricingTableProvider>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IWalkRepository, WalkRepository>();
        services.AddScoped<ITrackPointRepository, TrackPointRepository>();
        services.AddScoped<IWalkAlertRepository, WalkAlertRepository>();
        services.AddScoped<IWalkMessageRepository, WalkMessageRepository>();
        services.AddScoped<IWalkerProfileRepository, WalkerProfileRepository>();
        services.AddScoped<IWalkPaymentRepository, WalkPaymentRepository>();

        services.AddHealthChecks().AddDbContextCheck<WaggoDbContext>("postgres", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Brings the database schema up to date. Only for Development and Testing: production runs the migrations as a
    /// deploy step, never from the application.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
