using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Extensions;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Identity;
using Waggo.Application.Identity.Commands.ConfirmEmail;
using Waggo.Application.Identity.Commands.Login;
using Waggo.Application.Identity.Commands.RefreshSession;
using Waggo.Application.Identity.Commands.Register;
using Waggo.Application.Identity.Commands.VerifyLoginCode;
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

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithRequestValidation<LoginRequest>()
            .Produces<WaggoApiResponse<LoginResponse>>();

        group.MapPost("/login/verify", VerifyLoginCodeAsync)
            .WithName("VerifyLoginCode")
            .WithRequestValidation<VerifyLoginCodeRequest>()
            .Produces<WaggoApiResponse<SessionResponse>>();

        group.MapPost("/refresh", RefreshAsync)
            .WithName("Refresh")
            .WithRequestValidation<RefreshRequest>()
            .Produces<WaggoApiResponse<SessionResponse>>();

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

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ICommandHandler<LoginCommand, LoginResponse> handler,
        ILogger<LoginRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<LoginResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "Login");
        return response.ToApiResult();
    }

    private static async Task<IResult> VerifyLoginCodeAsync(
        VerifyLoginCodeRequest request,
        ICommandHandler<VerifyLoginCodeCommand, SessionResponse> handler,
        ILogger<VerifyLoginCodeRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<SessionResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "VerifyLoginCode");
        return response.ToApiResult();
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        ICommandHandler<RefreshSessionCommand, SessionResponse> handler,
        ILogger<RefreshRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<SessionResponse> response = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        response.WriteLogs(logger, "Refresh");
        return response.ToApiResult();
    }
}
