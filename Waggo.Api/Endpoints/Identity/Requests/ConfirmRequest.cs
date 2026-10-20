using Waggo.Application.Identity.Commands.ConfirmEmail;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Query string of <c>GET /api/v1/auth/confirm</c>. Raw value: it is validated before use.</summary>
public sealed record ConfirmRequest(string? Token)
{
    /// <summary>Only call it after the request passed <c>ConfirmRequestValidator</c>.</summary>
    public ConfirmEmailCommand ToCommand() => new(Token!);
}
