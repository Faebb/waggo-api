namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>Issues the API's own JWT access tokens (ADR-016, RNF-002).</summary>
public interface ITokenService
{
    /// <summary>The <c>sub</c> claim is <paramref name="userId"/>; roles go in the role claim configured for validation.</summary>
    (string AccessToken, int ExpiresInSeconds) IssueAccessToken(Guid userId, IReadOnlyList<string> roles);
}
