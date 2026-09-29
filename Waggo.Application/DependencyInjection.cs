using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Application.Abstractions;
using Waggo.Application.Pricing.QuoteFare;

namespace Waggo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> of this assembly (validators are internal sealed).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IQueryHandler<QuoteFareQuery, FareQuoteResponse>, QuoteFareHandler>();
        return services;
    }
}
