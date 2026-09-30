using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Payments;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Payments.Queries.GetWalkPayment;

/// <summary>RF-016: the owner and the assigned walker see whether the money is held, charged or released.</summary>
internal sealed class GetWalkPaymentHandler(
    IWalkRepository walks,
    IWalkPaymentRepository payments,
    ICurrentUser currentUser)
    : IQueryHandler<GetWalkPaymentQuery, WalkPaymentResponse>
{
    public async Task<WaggoResponse<WalkPaymentResponse>> HandleAsync(
        GetWalkPaymentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.WalkId, cancellationToken);
        if (walk?.PartyOf(currentUser.Id) is null)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.WalkId} is not visible to {currentUser.Id}");
        }

        WalkPayment payment = await payments.GetByWalkAsync(walk.Id, cancellationToken)
            ?? throw new NotFoundException(PaymentErrors.NotFound, $"Walk {walk.Id} has no payment");
        return new WaggoResponse<WalkPaymentResponse> { Data = WalkPaymentResponse.From(payment) };
    }
}
