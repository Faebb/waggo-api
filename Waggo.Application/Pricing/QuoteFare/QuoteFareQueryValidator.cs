using FluentValidation;
using Waggo.Application.Common.Validation;
using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing.QuoteFare;

/// <summary>Input rules of <see cref="QuoteFareQuery"/> (RF-019).</summary>
internal sealed class QuoteFareQueryValidator : AbstractValidator<QuoteFareQuery>
{
    public QuoteFareQueryValidator()
    {
        RuleFor(query => query.WalkType)
            .IsInEnum()
            .WithError(PricingErrors.InvalidWalkType);

        RuleFor(query => query.DurationMinutes)
            .Must(minutes => minutes is >= WalkDuration.MinMinutes and <= WalkDuration.MaxMinutes
                && minutes % WalkDuration.StepMinutes == 0)
            .WithError(PricingErrors.InvalidDuration);
    }
}
