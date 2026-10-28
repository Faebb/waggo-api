using Waggo.Application.Identity.Commands.LoginWithExternalProvider;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>Body of <c>POST /api/v1/auth/login/microsoft</c>: the <c>idToken</c> the person got from Microsoft (ADR-017).</summary>
public sealed record MicrosoftLoginRequest(string? IdToken)
{
    /// <summary>Only call it after the request passed <c>MicrosoftLoginRequestValidator</c>.</summary>
    public LoginWithExternalProviderCommand ToCommand() => new(ExternalProvider.Microsoft, IdToken!);
}
