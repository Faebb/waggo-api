namespace Waggo.Application.Pricing.Queries.QuoteFare;

public sealed record FareQuoteResponse(
    string WalkType,
    int DurationMinutes,
    string Currency,
    decimal Total,
    decimal Commission,
    decimal WalkerPayout);
