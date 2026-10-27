using Waggo.Application.Identity.Commands.ResetPassword;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/reset-password</c>. Raw values: they are validated before use.</summary>
public sealed record ResetPasswordRequest(string? Email, string? Code, string? NewPassword)
{
    /// <summary>Only call it after the request passed <c>ResetPasswordRequestValidator</c>.</summary>
    public ResetPasswordCommand ToCommand() => new(Email!, Code!, NewPassword!);
}
