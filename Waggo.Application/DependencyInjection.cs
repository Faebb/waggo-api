using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Commands.RegisterPet;
using Waggo.Application.Pets.Queries.GetPet;
using Waggo.Application.Pets.Queries.ListMyPets;
using Waggo.Application.Pricing.Queries.QuoteFare;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.CancelWalk;
using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Application.Walks.Queries.GetWalk;
using Waggo.Application.Walks.Queries.ListMyWalks;

namespace Waggo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> of this assembly (validators are internal sealed).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IQueryHandler<QuoteFareQuery, FareQuoteResponse>, QuoteFareHandler>();

        services.AddScoped<ICommandHandler<RegisterPetCommand, PetResponse>, RegisterPetHandler>();
        services.AddScoped<IQueryHandler<ListMyPetsQuery, IReadOnlyList<PetResponse>>, ListMyPetsHandler>();
        services.AddScoped<IQueryHandler<GetPetQuery, PetResponse>, GetPetHandler>();

        services.AddScoped<ICommandHandler<RequestWalkCommand, WalkResponse>, RequestWalkHandler>();
        services.AddScoped<ICommandHandler<CancelWalkCommand, WalkResponse>, CancelWalkHandler>();
        services.AddScoped<IQueryHandler<ListMyWalksQuery, IReadOnlyList<WalkResponse>>, ListMyWalksHandler>();
        services.AddScoped<IQueryHandler<GetWalkQuery, WalkResponse>, GetWalkHandler>();
        return services;
    }
}
