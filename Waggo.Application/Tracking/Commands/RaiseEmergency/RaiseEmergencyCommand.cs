using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Tracking.Commands.RaiseEmergency;

/// <summary>Message and position are optional: in an emergency, one tap must be enough.</summary>
public sealed record RaiseEmergencyCommand(Guid WalkId, string? Message, double? Latitude, double? Longitude)
    : ICommand<WalkAlertResponse>;
