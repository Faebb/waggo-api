using Waggo.Application.Identity.Commands.LoginWithExternalProvider;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/login/google</c>: the <c>idToken</c> the phone got from Google (ADR-017).</summary>
public sealed record GoogleLoginRequest(string? IdToken)
{
    /// <summary>Only call it after the request passed <c>GoogleLoginRequestValidator</c>.</summary>
    public LoginWithExternalProviderCommand ToCommand() => new(ExternalProvider.Google, IdToken!);
}
