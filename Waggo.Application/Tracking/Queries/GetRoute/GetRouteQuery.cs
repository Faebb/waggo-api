using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Tracking.Queries.GetRoute;

public sealed record GetRouteQuery(Guid WalkId) : IQuery<RouteResponse>;
