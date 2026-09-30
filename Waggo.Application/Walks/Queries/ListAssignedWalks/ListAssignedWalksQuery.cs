using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Queries.ListAssignedWalks;

/// <summary>Walks the current walker accepted, next one first.</summary>
public sealed record ListAssignedWalksQuery : IQuery<IReadOnlyList<WalkResponse>>;
