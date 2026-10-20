using Waggo.Api.Endpoints.Walks.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.AcceptWalk;
using Waggo.Application.Walks.Commands.CancelWalk;
using Waggo.Application.Walks.Commands.FinishWalk;
using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Application.Walks.Commands.StartWalk;
using Waggo.Application.Walks.Queries.GetWalk;
using Waggo.Application.Walks.Queries.ListAssignedWalks;
using Waggo.Application.Walks.Queries.ListAvailableWalks;
using Waggo.Application.Walks.Queries.ListMyWalks;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Walks;

/// <summary>
/// RF-007. Owner side: request a walk, see my walks and cancel one. Walker side: see open requests, accept one and
/// see the accepted ones. A walk's detail is visible to its owner and to its walker.
/// </summary>
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

        owner.MapPost("/{id:guid}/cancel", CancelWalkAsync)
            .WithName("CancelWalk")
            .Produces<WaggoApiResponse<WalkResponse>>();

        // Owner or assigned walker: the handler checks who the caller is.
        routes.MapGet("/walks/{id:guid}", GetWalkAsync)
            .WithTags("Walks")
            .WithName("GetWalk")
            .RequireAuthorization()
            .Produces<WaggoApiResponse<WalkResponse>>();

        RouteGroupBuilder walker = routes.MapGroup("/walks")
            .WithTags("Walks")
            .RequireAuthorization(WaggoPolicies.Walker);

        walker.MapGet("/available", ListAvailableWalksAsync)
            .WithName("ListAvailableWalks")
            .Produces<WaggoApiResponse<IReadOnlyList<AvailableWalkResponse>>>();

        walker.MapGet("/assigned", ListAssignedWalksAsync)
            .WithName("ListAssignedWalks")
            .Produces<WaggoApiResponse<IReadOnlyList<WalkResponse>>>();

        walker.MapPost("/{id:guid}/accept", AcceptWalkAsync)
            .WithName("AcceptWalk")
            .Produces<WaggoApiResponse<WalkResponse>>();

        // RF-008: the assigned walker starts the walk when picking the dogs up and finishes it when bringing them back.
        walker.MapPost("/{id:guid}/start", StartWalkAsync)
            .WithName("StartWalk")
            .Produces<WaggoApiResponse<WalkResponse>>();

        walker.MapPost("/{id:guid}/finish", FinishWalkAsync)
            .WithName("FinishWalk")
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

    private static async Task<IResult> ListAvailableWalksAsync(
        double? latitude,
        double? longitude,
        IQueryHandler<ListAvailableWalksQuery, IReadOnlyList<AvailableWalkResponse>> handler,
        ILogger<ListAvailableWalksQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<AvailableWalkResponse>> response =
            await handler.HandleAsync(new ListAvailableWalksQuery(latitude, longitude), cancellationToken);
        response.WriteLogs(logger, "ListAvailableWalks");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListAssignedWalksAsync(
        IQueryHandler<ListAssignedWalksQuery, IReadOnlyList<WalkResponse>> handler,
        ILogger<ListAssignedWalksQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<WalkResponse>> response =
            await handler.HandleAsync(new ListAssignedWalksQuery(), cancellationToken);
        response.WriteLogs(logger, "ListAssignedWalks");
        return response.ToApiResult();
    }

    private static async Task<IResult> AcceptWalkAsync(
        Guid id,
        ICommandHandler<AcceptWalkCommand, WalkResponse> handler,
        ILogger<AcceptWalkCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(new AcceptWalkCommand(id), cancellationToken);
        response.WriteLogs(logger, "AcceptWalk");
        return response.ToApiResult();
    }

    private static async Task<IResult> StartWalkAsync(
        Guid id,
        ICommandHandler<StartWalkCommand, WalkResponse> handler,
        ILogger<StartWalkCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(new StartWalkCommand(id), cancellationToken);
        response.WriteLogs(logger, "StartWalk");
        return response.ToApiResult();
    }

    private static async Task<IResult> FinishWalkAsync(
        Guid id,
        ICommandHandler<FinishWalkCommand, WalkResponse> handler,
        ILogger<FinishWalkCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkResponse> response = await handler.HandleAsync(new FinishWalkCommand(id), cancellationToken);
        response.WriteLogs(logger, "FinishWalk");
        return response.ToApiResult();
    }
}
