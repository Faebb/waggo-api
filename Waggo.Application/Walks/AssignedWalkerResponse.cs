namespace Waggo.Application.Walks;

/// <summary>
/// Who the assigned walker is (RF-007, RF-008), so the owner knows who picks the dogs up. Only the owner and the
/// walker of the walk see it. <see cref="FullName"/> is null only when the walker has no profile (walks older than
/// RF-002).
/// </summary>
public sealed record AssignedWalkerResponse(string Id, string? FullName);
