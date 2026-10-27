using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.ChangePassword;
using Waggo.Application.Identity.Commands.UpdateMyProfile;
using Waggo.Application.Identity.Queries.GetMyProfile;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Identity;

/// <summary>Perfil de usuario: the signed-in account's own email, full name and phone. Any role may call these.</summary>
internal static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/users/me")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/", GetMineAsync)
            .WithName("GetMyProfile")
            .Produces<WaggoApiResponse<UserProfileResponse>>();

        group.MapPut("/", UpdateMineAsync)
            .WithName("UpdateMyProfile")
            .Produces<WaggoApiResponse<UserProfileResponse>>();

        group.MapPut("/password", ChangePasswordAsync)
            .WithName("ChangeMyPassword")
            .WithRequestValidation<ChangePasswordRequest>()
            .Produces<WaggoApiResponse<bool>>();

        return routes;
    }

    private static async Task<IResult> GetMineAsync(
        IQueryHandler<GetMyProfileQuery, UserProfileResponse> handler,
        ILogger<GetMyProfileQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<UserProfileResponse> response = await handler.HandleAsync(new GetMyProfileQuery(), cancellationToken);
        response.WriteLogs(logger, "GetMyProfile");
        return response.ToApiResult();
    }

    private static async Task<IResult> UpdateMineAsync(
        UpdateMyProfileRequest request,
        ICommandHandler<UpdateMyProfileCommand, UserProfileResponse> handler,
        ILogger<UpdateMyProfileRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<UserProfileResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "UpdateMyProfile");
        return response.ToApiResult();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ICommandHandler<ChangePasswordCommand, bool> handler,
        ILogger<ChangePasswordRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<bool> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "ChangePassword");
        return response.ToApiResult();
    }
}
