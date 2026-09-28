using Waggo.Application.Abstractions;
using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing.QuoteFare;

public sealed record QuoteFareQuery(WalkType WalkType, int DurationMinutes) : IQuery<FareQuoteResponse>;
