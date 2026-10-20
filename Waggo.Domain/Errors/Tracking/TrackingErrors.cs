using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Services.Tracking;

namespace Waggo.Domain.Errors.Tracking;

public static class TrackingErrors
{
    public static readonly Error InvalidBatch = new(
        "Tracking.InvalidBatch",
        $"Envía de 1 a {RouteMetrics.MaxPointsPerBatch} posiciones por vez.");

    public static readonly Error WalkNotInProgress = new(
        "Tracking.WalkNotInProgress",
        "Solo se registra la ruta mientras el paseo está en curso.",
        ErrorType.BusinessRule);
}
