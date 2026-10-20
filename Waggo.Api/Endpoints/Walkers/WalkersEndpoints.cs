using Waggo.Api.Endpoints.Walkers.Requests;
using Waggo.Api.Infrastructure.Authorization;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Walkers;
using Waggo.Application.Walkers.Commands.ApproveWalker;
using Waggo.Application.Walkers.Commands.RegisterWalker;
using Waggo.Application.Walkers.Commands.RejectWalker;
using Waggo.Application.Walkers.Queries.GetMyWalkerProfile;
using Waggo.Application.Walkers.Queries.ListWalkersForReview;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Api.Endpoints.Walkers;

/// <summary>RF-002: walkers register their profile. RF-003: admins verify them (until a provider is chosen).</summary>
internal static class WalkersEndpoints
{
    public static IEndpointRouteBuilder MapWalkersEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder me = routes.MapGroup("/walkers/me")
            .WithTags("Walkers")
            .RequireAuthorization(WaggoPolicies.Walker);

        me.MapPost("/", RegisterAsync)
            .WithName("RegisterWalker")
            .WithRequestValidation<RegisterWalkerRequest>()
            .Produces<WaggoApiResponse<WalkerProfileResponse>>();

        me.MapGet("/", GetMineAsync)
            .WithName("GetMyWalkerProfile")
            .Produces<WaggoApiResponse<WalkerProfileResponse>>();

        RouteGroupBuilder admin = routes.MapGroup("/admin/walkers")
            .WithTags("Admin")
            .RequireAuthorization(WaggoPolicies.Admin);

        admin.MapGet("/", ListForReviewAsync)
            .WithName("ListWalkersForReview")
            .Produces<WaggoApiResponse<IReadOnlyList<WalkerProfileResponse>>>();

        admin.MapPost("/{id:guid}/approve", ApproveAsync)
            .WithName("ApproveWalker")
            .Produces<WaggoApiResponse<WalkerProfileResponse>>();

        admin.MapPost("/{id:guid}/reject", RejectAsync)
            .WithName("RejectWalker")
            .Produces<WaggoApiResponse<WalkerProfileResponse>>();

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterWalkerRequest request,
        ICommandHandler<RegisterWalkerCommand, WalkerProfileResponse> handler,
        ILogger<RegisterWalkerRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkerProfileResponse> response =
            await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "RegisterWalker");
        return response.ToApiResult();
    }

    private static async Task<IResult> GetMineAsync(
        IQueryHandler<GetMyWalkerProfileQuery, WalkerProfileResponse> handler,
        ILogger<GetMyWalkerProfileQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkerProfileResponse> response =
            await handler.HandleAsync(new GetMyWalkerProfileQuery(), cancellationToken);
        response.WriteLogs(logger, "GetMyWalkerProfile");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListForReviewAsync(
        VerificationStatus? status,
        IQueryHandler<ListWalkersForReviewQuery, IReadOnlyList<WalkerProfileResponse>> handler,
        ILogger<ListWalkersForReviewQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<WalkerProfileResponse>> response =
            await handler.HandleAsync(new ListWalkersForReviewQuery(status), cancellationToken);
        response.WriteLogs(logger, "ListWalkersForReview");
        return response.ToApiResult();
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        ICommandHandler<ApproveWalkerCommand, WalkerProfileResponse> handler,
        ILogger<ApproveWalkerCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkerProfileResponse> response =
            await handler.HandleAsync(new ApproveWalkerCommand(id), cancellationToken);
        response.WriteLogs(logger, "ApproveWalker");
        return response.ToApiResult();
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        RejectWalkerRequest request,
        ICommandHandler<RejectWalkerCommand, WalkerProfileResponse> handler,
        ILogger<RejectWalkerCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkerProfileResponse> response =
            await handler.HandleAsync(new RejectWalkerCommand(id, request.Reason ?? string.Empty), cancellationToken);
        response.WriteLogs(logger, "RejectWalker");
        return response.ToApiResult();
    }
}
