using Waggo.Application.Identity.Commands.VerifyLoginCode;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/login/verify</c>. Raw values: they are validated before use.</summary>
public sealed record VerifyLoginCodeRequest(Guid? ChallengeId, string? Code)
{
    /// <summary>Only call it after the request passed <c>VerifyLoginCodeRequestValidator</c>.</summary>
    public VerifyLoginCodeCommand ToCommand() => new(ChallengeId!.Value, Code!);
}
