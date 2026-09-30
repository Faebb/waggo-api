using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Tracking.Queries.ListWalkAlerts;

/// <summary>Alerts of a walk, newest first.</summary>
public sealed record ListWalkAlertsQuery(Guid WalkId) : IQuery<IReadOnlyList<WalkAlertResponse>>;
