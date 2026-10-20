using Waggo.Domain.Entities.Tracking;

namespace Waggo.Application.Tracking;

/// <summary>An alert of a walk as the owner and the walker see it.</summary>
public sealed record WalkAlertResponse(
    Guid Id,
    string Kind,
    string? RaisedBy,
    string? Message,
    double? Latitude,
    double? Longitude,
    DateTimeOffset RaisedAt)
{
    public static WalkAlertResponse From(WalkAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return new WalkAlertResponse(
            alert.Id,
            alert.Kind.ToString(),
            alert.RaisedBy?.ToString(),
            alert.Message,
            alert.Location?.Latitude,
            alert.Location?.Longitude,
            alert.RaisedAt);
    }
}
