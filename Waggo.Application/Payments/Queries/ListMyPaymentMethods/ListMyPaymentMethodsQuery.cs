using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Payments.Queries.ListMyPaymentMethods;

/// <summary>Payment methods of the current owner. Not paged: an owner has only a few.</summary>
public sealed record ListMyPaymentMethodsQuery : IQuery<IReadOnlyList<PaymentMethodResponse>>;
