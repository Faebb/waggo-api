using Waggo.Domain.Common;
using Waggo.Domain.Enums.Tracking;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.Entities.Tracking;

/// <summary>Something that needs attention during a walk (RF-012 emergencies; later geofences and anomalies).</summary>
public sealed class WalkAlert
{
    public const int MaxMessageLength = 500;

    // Used by EF Core to materialize the entity.
    private WalkAlert()
    {
    }

    public Guid Id { get; private set; }

    public Guid WalkId { get; private set; }

    public AlertKind Kind { get; private set; }

    /// <summary>Who raised it; null when the platform raised it (geofence, anomaly).</summary>
    public WalkParty? RaisedBy { get; private set; }

    public string? Message { get; private set; }

    /// <summary>Where it happened, when the phone could tell.</summary>
    public GeoPoint? Location { get; private set; }

    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>The platform detected something from the route (RF-009, RF-010).</summary>
    public static WalkAlert RaiseAutomatic(Guid walkId, AlertKind kind, GeoPoint location, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(location);
        return new WalkAlert
        {
            Id = Guid.CreateVersion7(at),
            WalkId = walkId,
            Kind = kind,
            RaisedBy = null,
            Location = location,
            RaisedAt = at,
        };
    }

    /// <summary>The owner or the walker presses the emergency button. Message and location are optional.</summary>
    public static WaggoResponse<WalkAlert> RaiseEmergency(
        Guid walkId,
        WalkParty raisedBy,
        string? message,
        GeoPoint? location,
        DateTimeOffset now)
    {
        WaggoResponse<WalkAlert> response = new();
        string? trimmed = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        if (trimmed?.Length > MaxMessageLength)
        {
            response.AddError(AlertErrors.InvalidMessage);
            return response;
        }

        response.Data = new WalkAlert
        {
            Id = Guid.CreateVersion7(now),
            WalkId = walkId,
            Kind = AlertKind.Emergency,
            RaisedBy = raisedBy,
            Message = trimmed,
            Location = location,
            RaisedAt = now,
        };
        return response;
    }
}
