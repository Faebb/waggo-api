using FluentValidation;
using Waggo.Api.Endpoints.Pricing.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Api.Endpoints.Pricing.Validators;

/// <summary>
/// First layer (DTO): required fields and formats. Business rules (allowed durations) live in
/// <c>QuoteFareQueryValidator</c> in the Application layer.
/// </summary>
internal sealed class QuoteFareRequestValidator : AbstractValidator<QuoteFareRequest>
{
    private static readonly string s_walkTypes = string.Join(", ", Enum.GetNames<WalkType>());

    public QuoteFareRequestValidator()
    {
        RuleFor(request => request.WalkType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("walkType"))
            .Must(value => Enum.TryParse<WalkType>(value, ignoreCase: true, out _) && !int.TryParse(value, out _))
            .WithError(RequestErrors.InvalidValue("walkType", $"Usa uno de estos valores: {s_walkTypes}."));

        RuleFor(request => request.DurationMinutes)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithError(RequestErrors.Required("durationMinutes"))
            .GreaterThan(0)
            .WithError(RequestErrors.InvalidValue("durationMinutes", "Debe ser un número de minutos mayor que cero."));
    }
}
