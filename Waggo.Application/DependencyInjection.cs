using Microsoft.Extensions.DependencyInjection;
using Waggo.Application.Abstractions;
using Waggo.Application.Pricing.QuoteFare;

namespace Waggo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<QuoteFareQuery, FareQuoteResponse>, QuoteFareHandler>();
        return services;
    }
}
