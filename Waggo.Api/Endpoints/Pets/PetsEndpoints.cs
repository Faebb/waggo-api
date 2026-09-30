using Waggo.Api.Endpoints.Pets.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Commands.RegisterPet;
using Waggo.Application.Pets.Queries.GetPet;
using Waggo.Application.Pets.Queries.ListMyPets;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Pets;

/// <summary>RF-004: the owner registers their dogs and sees them. Always scoped to the current owner.</summary>
internal static class PetsEndpoints
{
    public static IEndpointRouteBuilder MapPetsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/pets")
            .WithTags("Pets")
            .RequireAuthorization(WaggoPolicies.Owner);

        group.MapPost("/", RegisterPetAsync)
            .WithName("RegisterPet")
            .WithRequestValidation<RegisterPetRequest>()
            .Produces<WaggoApiResponse<PetResponse>>();

        group.MapGet("/", ListMyPetsAsync)
            .WithName("ListMyPets")
            .Produces<WaggoApiResponse<IReadOnlyList<PetResponse>>>();

        group.MapGet("/{id:guid}", GetPetAsync)
            .WithName("GetPet")
            .Produces<WaggoApiResponse<PetResponse>>();

        return routes;
    }

    private static async Task<IResult> RegisterPetAsync(
        RegisterPetRequest request,
        ICommandHandler<RegisterPetCommand, PetResponse> handler,
        ILogger<RegisterPetRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<PetResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "RegisterPet");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListMyPetsAsync(
        IQueryHandler<ListMyPetsQuery, IReadOnlyList<PetResponse>> handler,
        ILogger<ListMyPetsQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<PetResponse>> response =
            await handler.HandleAsync(new ListMyPetsQuery(), cancellationToken);
        response.WriteLogs(logger, "ListMyPets");
        return response.ToApiResult();
    }

    private static async Task<IResult> GetPetAsync(
        Guid id,
        IQueryHandler<GetPetQuery, PetResponse> handler,
        ILogger<GetPetQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<PetResponse> response = await handler.HandleAsync(new GetPetQuery(id), cancellationToken);
        response.WriteLogs(logger, "GetPet");
        return response.ToApiResult();
    }
}
