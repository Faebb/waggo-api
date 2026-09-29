using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Application.Pricing;
using Waggo.Infrastructure.Persistence;
using Waggo.Infrastructure.Pricing;

namespace Waggo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Waggo")
            ?? throw new InvalidOperationException("Connection string 'Waggo' is not configured.");

        services.AddDbContext<WaggoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddOptions<PricingOptions>()
            .Bind(configuration.GetSection(PricingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPricingTableProvider, ConfigurationPricingTableProvider>();

        services.AddHealthChecks().AddDbContextCheck<WaggoDbContext>("postgres", tags: ["ready"]);

        return services;
    }
}
