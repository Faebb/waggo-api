using Waggo.Application.Identity.Commands.ResendConfirmation;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/resend-confirmation</c>. Raw value: it is validated before use.</summary>
public sealed record ResendConfirmationRequest(string? Email)
{
    /// <summary>Only call it after the request passed <c>ResendConfirmationRequestValidator</c>.</summary>
    public ResendConfirmationCommand ToCommand() => new(Email!);
}
