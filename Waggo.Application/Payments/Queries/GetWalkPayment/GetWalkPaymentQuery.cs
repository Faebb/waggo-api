using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Payments.Queries.GetWalkPayment;

public sealed record GetWalkPaymentQuery(Guid WalkId) : IQuery<WalkPaymentResponse>;
