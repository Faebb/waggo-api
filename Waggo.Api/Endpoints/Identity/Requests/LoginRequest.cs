using Waggo.Application.Identity.Commands.Login;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/login</c>. Raw values: they are validated before use.</summary>
public sealed record LoginRequest(string? Email, string? Password)
{
    /// <summary>Only call it after the request passed <c>LoginRequestValidator</c>.</summary>
    public LoginCommand ToCommand() => new(Email!, Password!);
}
