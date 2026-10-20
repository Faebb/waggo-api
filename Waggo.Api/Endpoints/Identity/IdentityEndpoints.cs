using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.ConfirmEmail;
using Waggo.Application.Identity.Commands.Register;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Identity;

/// <summary>
/// RF-001: create and confirm an account. The only endpoints of the API open to anyone (<c>AllowAnonymous</c>);
/// everything else requires the session this module will issue (RF-005, next slice).
/// </summary>
internal static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/auth")
            .WithTags("Identity")
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithRequestValidation<RegisterRequest>()
            .Produces<WaggoApiResponse<UserResponse>>();

        group.MapGet("/confirm", ConfirmAsync)
            .WithName("ConfirmEmail")
            .WithRequestValidation<ConfirmRequest>()
            .Produces<WaggoApiResponse<bool>>();

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ICommandHandler<RegisterCommand, UserResponse> handler,
        ILogger<RegisterRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<UserResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "Register");
        return response.ToApiResult();
    }

    private static async Task<IResult> ConfirmAsync(
        [AsParameters] ConfirmRequest request,
        ICommandHandler<ConfirmEmailCommand, bool> handler,
        ILogger<ConfirmRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<bool> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "ConfirmEmail");
        return response.ToApiResult();
    }
}
