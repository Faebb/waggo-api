using FluentValidation;
using Waggo.Api.Endpoints.Walks.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Api.Endpoints.Walks.Validators;

/// <summary>
/// First layer (DTO): required fields and formats. Business rules (pets of the owner, duration, schedule, location)
/// live in <c>RequestWalkCommandValidator</c>, the handler and the <c>Walk</c> entity.
/// </summary>
internal sealed class RequestWalkRequestValidator : AbstractValidator<RequestWalkRequest>
{
    private static readonly string s_walkTypes = string.Join(", ", Enum.GetNames<WalkType>());

    public RequestWalkRequestValidator()
    {
        RuleFor(request => request.PetIds)
            .NotEmpty()
            .WithError(RequestErrors.Required("petIds"));

        RuleFor(request => request.WalkType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("walkType"))
            .Must(value => Enum.TryParse<WalkType>(value, ignoreCase: true, out _) && !int.TryParse(value, out _))
            .WithError(RequestErrors.InvalidValue("walkType", $"Usa uno de estos valores: {s_walkTypes}."));

        RuleFor(request => request.DurationMinutes)
            .NotNull()
            .WithError(RequestErrors.Required("durationMinutes"));

        RuleFor(request => request.PickupAddress)
            .NotEmpty()
            .WithError(RequestErrors.Required("pickupAddress"));

        RuleFor(request => request.Latitude)
            .NotNull()
            .WithError(RequestErrors.Required("latitude"));

        RuleFor(request => request.Longitude)
            .NotNull()
            .WithError(RequestErrors.Required("longitude"));
    }
}
