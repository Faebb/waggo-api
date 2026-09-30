using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Application.Common.Models.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Commands.FinishWalk;

/// <summary>
/// RF-008/RF-011: the assigned walker brought the dogs back and finishes the walk. The payment is captured and split
/// between Waggo and the walker (RF-017, RF-018).
/// </summary>
internal sealed class FinishWalkHandler(
    IWalkRepository walks,
    IWalkPaymentRepository payments,
    IPaymentGateway gateway,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<FinishWalkCommand, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(
        FinishWalkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkResponse> response = new();

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        if (walk is null || walk.WalkerId != currentUser.Id)
        {
            throw new NotFoundException(
                WalkErrors.NotFound,
                $"Walk {command.WalkId} is not assigned to {currentUser.Id}");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        response.ConcatStacks(walk.Finish(now));
        if (!response.IsValid)
        {
            return response;
        }

        // Walks requested before payments existed have no payment.
        WalkPayment? payment = await payments.GetByWalkAsync(walk.Id, cancellationToken);
        if (payment is not null)
        {
            response.ConcatStacks(payment.Capture(currentUser.Id, now));
            if (!response.IsValid)
            {
                return response;
            }

            await gateway.CaptureAsync(
                new PaymentCapture(
                    payment.GatewayReference,
                    payment.Total,
                    payment.Commission,
                    currentUser.Id,
                    payment.WalkerPayout,
                    payment.Currency),
                cancellationToken);
        }

        await walks.SaveChangesAsync(cancellationToken);
        response.Data = WalkResponse.From(walk);
        return response;
    }
}
