using Waggo.Domain.Common;

namespace Waggo.Domain.Pricing;

public static class PricingErrors
{
    public static readonly Error InvalidDuration = new(
        "Pricing.InvalidDuration",
        $"La duración del paseo debe estar entre {WalkDuration.MinMinutes} y {WalkDuration.MaxMinutes} minutos, "
        + $"en intervalos de {WalkDuration.StepMinutes}.");

    public static readonly Error InvalidWalkType = new(
        "Pricing.InvalidWalkType",
        "El tipo de paseo no es válido.");

    public static readonly Error InvalidCommissionRate = new(
        "Pricing.InvalidCommissionRate",
        "La comisión debe estar entre 0 y 1.");

    public static Error WalkTypeNotPriced(WalkType walkType) => new(
        "Pricing.WalkTypeNotPriced",
        $"No hay una tarifa configurada para el tipo de paseo '{walkType}'.");
}
