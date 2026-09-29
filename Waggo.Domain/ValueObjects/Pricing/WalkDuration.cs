using Waggo.Domain.Common;
using Waggo.Domain.Errors.Pricing;

namespace Waggo.Domain.ValueObjects.Pricing;

/// <summary>Duration of a walk. Valid range: 30–120 minutes in steps of 15.</summary>
public sealed record WalkDuration
{
    public const int MinMinutes = 30;
    public const int MaxMinutes = 120;
    public const int StepMinutes = 15;

    private WalkDuration(int minutes) => Minutes = minutes;

    public int Minutes { get; }

    public static WaggoResponse<WalkDuration> Create(int minutes)
    {
        WaggoResponse<WalkDuration> response = new();

        if (minutes is < MinMinutes or > MaxMinutes || minutes % StepMinutes != 0)
        {
            response.AddError(PricingErrors.InvalidDuration);
            return response;
        }

        response.Data = new WalkDuration(minutes);
        return response;
    }
}
