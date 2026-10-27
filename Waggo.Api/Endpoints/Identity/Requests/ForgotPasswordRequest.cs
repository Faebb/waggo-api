using Waggo.Application.Identity.Commands.ForgotPassword;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/forgot-password</c>. Raw value: it is validated before use.</summary>
public sealed record ForgotPasswordRequest(string? Email)
{
    /// <summary>Only call it after the request passed <c>ForgotPasswordRequestValidator</c>.</summary>
    public ForgotPasswordCommand ToCommand() => new(Email!);
}
