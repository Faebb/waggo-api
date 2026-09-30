using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Common.Interfaces.Pricing;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Common.Models.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Services.Pricing;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.Walks.Commands.RequestWalk;

/// <summary>
/// RF-007: the current owner requests a walk for some of their dogs. The fare is quoted with the current rates
/// (RF-019) and frozen in the walk, which starts looking for a walker. The total is held on the owner's payment
/// method first (RF-016): if the gateway declines, no walk is created.
/// </summary>
internal sealed class RequestWalkHandler(
    IValidator<RequestWalkCommand> validator,
    IWalkRepository walks,
    IPetRepository pets,
    IPricingTableProvider pricingTableProvider,
    IWalkPaymentRepository payments,
    IPaymentGateway gateway,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RequestWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        RequestWalkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkResponse> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        IReadOnlyList<Pet> ownerPets = await pets.ListByOwnerAsync(currentUser.Id, cancellationToken);
        if (!command.PetIds.All(id => ownerPets.Any(pet => pet.Id == id)))
        {
            response.AddError(WalkErrors.InvalidPets);
            return response;
        }

        WaggoResponse<WalkDuration> duration = WalkDuration.Create(command.DurationMinutes);
        WaggoResponse<GeoPoint> location = GeoPoint.Create(command.Latitude, command.Longitude);
        response.ConcatStacks(duration);
        response.ConcatStacks(location);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<PricingTable> table = await pricingTableProvider.GetCurrentAsync(cancellationToken);
        response.ConcatStacks(table);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<FareBreakdown> fare = FareCalculator.Calculate(table.Data, command.WalkType, duration.Data);
        response.ConcatStacks(fare);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<Walk> walk = Walk.Request(
            currentUser.Id,
            command.PetIds,
            command.WalkType,
            duration.Data,
            command.PickupAddress,
            location.Data,
            command.ScheduledFor,
            command.Notes,
            fare.Data,
            timeProvider.GetUtcNow());
        response.ConcatStacks(walk);
        if (!response.IsValid)
        {
            return response;
        }

        WaggoResponse<string> hold = await gateway.HoldAsync(
            new PaymentHold(walk.Data.Id, currentUser.Id, walk.Data.Total, walk.Data.Currency),
            cancellationToken);
        response.ConcatStacks(hold);
        if (!response.IsValid)
        {
            return response;
        }

        await walks.AddAsync(walk.Data, cancellationToken);
        await payments.AddAsync(WalkPayment.Hold(walk.Data, hold.Data, timeProvider.GetUtcNow()), cancellationToken);
        response.Data = WalkResponse.From(walk.Data);
        return response;
    }
}
