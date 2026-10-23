using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Payments.Queries.ListMyPaymentMethods;

/// <summary>RF-020: the owner sees only their own methods, the default first.</summary>
internal sealed class ListMyPaymentMethodsHandler(IPaymentMethodRepository methods, ICurrentUser currentUser)
    : IQueryHandler<ListMyPaymentMethodsQuery, IReadOnlyList<PaymentMethodResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<PaymentMethodResponse>>> HandleAsync(
        ListMyPaymentMethodsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PaymentMethod> mine = await methods.ListByOwnerAsync(currentUser.Id, cancellationToken);
        return new WaggoResponse<IReadOnlyList<PaymentMethodResponse>> { Data = [.. mine.Select(PaymentMethodResponse.From)] };
    }
}
