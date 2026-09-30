using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Tracking;

public static class AlertErrors
{
    public static readonly Error InvalidMessage = new(
        "Alerts.InvalidMessage",
        $"El mensaje puede tener hasta {WalkAlert.MaxMessageLength} caracteres.");

    public static readonly Error WalkNotActive = new(
        "Alerts.WalkNotActive",
        "Solo puedes reportar una emergencia en un paseo aceptado o en curso.",
        ErrorType.BusinessRule);
}
