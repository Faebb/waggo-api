using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Application.Pricing.Queries.QuoteFare;

public sealed record QuoteFareQuery(WalkType WalkType, int DurationMinutes) : IQuery<FareQuoteResponse>;
