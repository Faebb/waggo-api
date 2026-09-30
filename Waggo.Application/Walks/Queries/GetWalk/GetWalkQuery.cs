using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Queries.GetWalk;

public sealed record GetWalkQuery(Guid Id) : IQuery<WalkResponse>;
