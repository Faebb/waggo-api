using Waggo.Application.Identity.Commands.Logout;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/logout</c>. Raw value: it is validated before use.</summary>
public sealed record LogoutRequest(string? RefreshToken)
{
    /// <summary>Only call it after the request passed <c>LogoutRequestValidator</c>.</summary>
    public LogoutCommand ToCommand() => new(RefreshToken!);
}
