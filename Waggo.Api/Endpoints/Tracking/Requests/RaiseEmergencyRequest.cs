using Waggo.Application.Tracking.Commands.RaiseEmergency;

namespace Waggo.Api.Endpoints.Tracking.Requests;

/// <summary>Body of <c>POST /api/v1/walks/{id}/emergency</c>. Every field is optional.</summary>
public sealed record RaiseEmergencyRequest(string? Message, double? Latitude, double? Longitude)
{
    public RaiseEmergencyCommand ToCommand(Guid walkId) => new(walkId, Message, Latitude, Longitude);
}
