using Waggo.Application.Identity.Commands.ResendLoginCode;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/resend-code</c>. Raw value: it is validated before use.</summary>
public sealed record ResendLoginCodeRequest(Guid? ChallengeId)
{
    /// <summary>Only call it after the request passed <c>ResendLoginCodeRequestValidator</c>.</summary>
    public ResendLoginCodeCommand ToCommand() => new(ChallengeId!.Value);
}
