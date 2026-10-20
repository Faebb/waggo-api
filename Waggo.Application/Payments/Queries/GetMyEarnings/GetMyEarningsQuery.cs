using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Payments.Queries.GetMyEarnings;

public sealed record GetMyEarningsQuery : IQuery<WalkerEarningsResponse>;
