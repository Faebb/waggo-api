using Waggo.Application.Identity.Commands.RequestEmailChange;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/users/me/email</c>. Raw values: they are validated before use.</summary>
public sealed record RequestEmailChangeRequest(string? NewEmail, string? Password)
{
    /// <summary>Only call it after the request passed <c>RequestEmailChangeRequestValidator</c>.</summary>
    public RequestEmailChangeCommand ToCommand() => new(NewEmail!, Password!);
}
