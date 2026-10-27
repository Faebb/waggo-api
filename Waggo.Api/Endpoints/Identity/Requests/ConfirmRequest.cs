using Waggo.Application.Identity.Commands.ConfirmEmail;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/confirm</c>. Raw values: they are validated before use.</summary>
public sealed record ConfirmRequest(string? Email, string? Code)
{
    /// <summary>Only call it after the request passed <c>ConfirmRequestValidator</c>.</summary>
    public ConfirmEmailCommand ToCommand() => new(Email!, Code!);
}
