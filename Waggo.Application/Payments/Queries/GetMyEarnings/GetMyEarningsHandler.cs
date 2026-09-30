using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Payments.Queries.GetMyEarnings;

/// <summary>RF-017: the walker sees what they earned, walk by walk.</summary>
internal sealed class GetMyEarningsHandler(IWalkPaymentRepository payments, ICurrentUser currentUser)
    : IQueryHandler<GetMyEarningsQuery, WalkerEarningsResponse>
{
    public async Task<WaggoResponse<WalkerEarningsResponse>> HandleAsync(
        GetMyEarningsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WalkPayment> captured =
            await payments.ListCapturedByWalkerAsync(currentUser.Id, cancellationToken);

        List<EarningResponse> walks =
        [
            .. captured.Select(payment =>
                new EarningResponse(payment.WalkId, payment.WalkerPayout, payment.CapturedAt!.Value)),
        ];
        return new WaggoResponse<WalkerEarningsResponse>
        {
            Data = new WalkerEarningsResponse(
                captured.Count > 0 ? captured[0].Currency : null,
                captured.Sum(payment => payment.WalkerPayout),
                walks),
        };
    }
}
