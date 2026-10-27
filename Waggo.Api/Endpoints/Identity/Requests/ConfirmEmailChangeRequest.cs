using Waggo.Application.Identity.Commands.ConfirmEmailChange;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>
/// Body of <c>POST /api/v1/users/me/email/confirm</c>. Raw values: they are validated before use. <c>RefreshToken</c>
/// is the current session's, so it stays open while the others are closed.
/// </summary>
public sealed record ConfirmEmailChangeRequest(string? Code, string? RefreshToken)
{
    /// <summary>Only call it after the request passed <c>ConfirmEmailChangeRequestValidator</c>.</summary>
    public ConfirmEmailChangeCommand ToCommand() => new(Code!, RefreshToken);
}
