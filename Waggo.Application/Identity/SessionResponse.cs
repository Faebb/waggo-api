namespace Waggo.Application.Identity;

public sealed record SessionResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
