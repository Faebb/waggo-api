using Waggo.Application.Identity.Commands.RefreshSession;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/refresh</c>. Raw value: it is validated before use.</summary>
public sealed record RefreshRequest(string? RefreshToken)
{
    /// <summary>Only call it after the request passed <c>RefreshRequestValidator</c>.</summary>
    public RefreshSessionCommand ToCommand() => new(RefreshToken!);
}
