using Waggo.Application.Identity.Commands.Register;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/register</c>. Raw values: they are validated before use.</summary>
public sealed record RegisterRequest(string? Email, string? Password)
{
    /// <summary>Only call it after the request passed <c>RegisterRequestValidator</c>.</summary>
    public RegisterCommand ToCommand() => new(Email!, Password!);
}
