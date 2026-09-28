using Waggo.Domain.Common;

namespace Waggo.Domain.Pricing;

/// <summary>Duration of a walk. Valid range: 30–120 minutes in steps of 15.</summary>
public sealed record WalkDuration
{
    public const int MinMinutes = 30;
    public const int MaxMinutes = 120;
    public const int StepMinutes = 15;

    private WalkDuration(int minutes) => Minutes = minutes;

    public int Minutes { get; }

    public static Result<WalkDuration> Create(int minutes)
    {
        if (minutes is < MinMinutes or > MaxMinutes || minutes % StepMinutes != 0)
        {
            return PricingErrors.InvalidDuration;
        }

        return new WalkDuration(minutes);
    }
}
