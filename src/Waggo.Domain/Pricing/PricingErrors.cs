using Waggo.Domain.Common;

namespace Waggo.Domain.Pricing;

public static class PricingErrors
{
    public static readonly Error InvalidDuration = new(
        "Pricing.InvalidDuration",
        $"Walk duration must be between {WalkDuration.MinMinutes} and {WalkDuration.MaxMinutes} minutes, in steps of {WalkDuration.StepMinutes}.");

    public static readonly Error InvalidCommissionRate = new(
        "Pricing.InvalidCommissionRate",
        "Commission rate must be between 0 and 1.");

    public static Error WalkTypeNotPriced(WalkType walkType) => new(
        "Pricing.WalkTypeNotPriced",
        $"There is no rate configured for walk type '{walkType}'.");
}
