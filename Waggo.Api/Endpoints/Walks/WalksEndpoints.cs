using Waggo.Api.Endpoints.Walks.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.CancelWalk;
using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Application.Walks.Queries.GetWalk;
using Waggo.Application.Walks.Queries.ListMyWalks;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Walks;

/// <summary>RF-007 (owner side): request a walk, see my walks and cancel one. Always scoped to the current owner.</summary>
internal static class WalksEndpoints
{
    public static IEndpointRouteBuilder MapWalksEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder owner = routes.MapGroup("/walks")
            .WithTags("Walks")
            .RequireAuthorization(WaggoPolicies.Owner);

        owner.MapPost("/", RequestWalkAsync)
            .WithName("RequestWalk")
            .WithRequestValidation<RequestWalkRequest>()
            .Produces<WaggoApiResponse<WalkResponse>>();

        owner.MapGet("/", ListMyWalksAsync)
            .WithName("ListMyWalks")
            .Produces<WaggoApiResponse<IReadOnlyList<WalkResponse>>>();

        owner.MapGet("/{id:guid}", GetWalkAsync)
            .WithName("GetWalk")
            .Produces<WaggoApiResponse<WalkResponse>>();

        owner.MapPost("/{id:guid}/cancel", CancelWalkAsync)
            .WithName("CancelWalk")
            .Produces<WaggoApiResponse<WalkResponse>>();

        return routes;
    }

    private static async Task<IResult> RequestWalkAsync(
        RequestWalkRequest request,
        ICommandHandler<RequestWalkCommand, WalkResponse> handler,
        ILogger<RequestWalkRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "RequestWalk");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListMyWalksAsync(
        IQueryHandler<ListMyWalksQuery, IReadOnlyList<WalkResponse>> handler,
        ILogger<ListMyWalksQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<WalkResponse>> response =
            await handler.HandleAsync(new ListMyWalksQuery(), cancellationToken);
        response.WriteLogs(logger, "ListMyWalks");
        return response.ToApiResult();
    }

    private static async Task<IResult> GetWalkAsync(
        Guid id,
        IQueryHandler<GetWalkQuery, WalkResponse> handler,
        ILogger<GetWalkQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(new GetWalkQuery(id), cancellationToken);
        response.WriteLogs(logger, "GetWalk");
        return response.ToApiResult();
    }

    private static async Task<IResult> CancelWalkAsync(
        Guid id,
        ICommandHandler<CancelWalkCommand, WalkResponse> handler,
        ILogger<CancelWalkCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(new CancelWalkCommand(id), cancellationToken);
        response.WriteLogs(logger, "CancelWalk");
        return response.ToApiResult();
    }
}
