namespace Waggo.Api.Infrastructure.Responses;

/// <summary>Public message (error, warning or info) sent to the client.</summary>
public sealed record WaggoApiMessage(string Code, string Message);
