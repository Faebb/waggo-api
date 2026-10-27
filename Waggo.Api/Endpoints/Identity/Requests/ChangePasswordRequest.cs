using Waggo.Application.Identity.Commands.ChangePassword;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>
/// Body of <c>PUT /api/v1/users/me/password</c>. Raw values: they are validated before use. <c>RefreshToken</c> is the
/// current session's, so it stays open while the others are closed.
/// </summary>
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? RefreshToken)
{
    /// <summary>Only call it after the request passed <c>ChangePasswordRequestValidator</c>.</summary>
    public ChangePasswordCommand ToCommand() => new(CurrentPassword!, NewPassword!, RefreshToken);
}
