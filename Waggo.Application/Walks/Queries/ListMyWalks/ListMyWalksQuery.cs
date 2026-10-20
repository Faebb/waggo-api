using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Queries.ListMyWalks;

/// <summary>Walks of the current owner, newest first.</summary>
public sealed record ListMyWalksQuery : IQuery<IReadOnlyList<WalkResponse>>;
