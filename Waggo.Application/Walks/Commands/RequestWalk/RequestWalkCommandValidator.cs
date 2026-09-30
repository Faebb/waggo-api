using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Pricing;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.ValueObjects.Pricing;

namespace Waggo.Application.Walks.Commands.RequestWalk;

/// <summary>
/// Field rules of <see cref="RequestWalkCommand"/> (RF-007). The schedule needs the clock, so the <see cref="Walk"/>
/// entity checks it when it is created; that the pets belong to the owner is checked by the handler.
/// </summary>
internal sealed class RequestWalkCommandValidator : AbstractValidator<RequestWalkCommand>
{
    public RequestWalkCommandValidator()
    {
        RuleFor(command => command.PetIds)
            .Must(ids => ids is { Count: > 0 and <= Walk.MaxPets } && ids.Distinct().Count() == ids.Count)
            .WithError(WalkErrors.InvalidPets);

        RuleFor(command => command.WalkType)
            .IsInEnum()
            .WithError(PricingErrors.InvalidWalkType);

        RuleFor(command => command.DurationMinutes)
            .Must(minutes => minutes is >= WalkDuration.MinMinutes and <= WalkDuration.MaxMinutes
                && minutes % WalkDuration.StepMinutes == 0)
            .WithError(PricingErrors.InvalidDuration);

        RuleFor(command => command.PickupAddress)
            .Must(address => address?.Trim().Length is >= Walk.MinAddressLength and <= Walk.MaxAddressLength)
            .WithError(WalkErrors.InvalidAddress);

        RuleFor(command => command)
            .Must(command => command.Latitude is >= -90 and <= 90 && command.Longitude is >= -180 and <= 180)
            .WithError(WalkErrors.InvalidLocation);

        RuleFor(command => command.Notes)
            .Must(notes => notes is null || notes.Trim().Length <= Walk.MaxNotesLength)
            .WithError(WalkErrors.InvalidNotes);
    }
}
