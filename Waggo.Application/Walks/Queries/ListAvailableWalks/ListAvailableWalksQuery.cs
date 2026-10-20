using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Queries.ListAvailableWalks;

/// <summary>Open requests for the current walker. With a position, the nearest come first.</summary>
public sealed record ListAvailableWalksQuery(double? Latitude, double? Longitude)
    : IQuery<IReadOnlyList<AvailableWalkResponse>>;
