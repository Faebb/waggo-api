namespace Waggo.Application.Pricing.QuoteFare;

public sealed record FareQuoteResponse(
    string WalkType,
    int DurationMinutes,
    string Currency,
    decimal Total,
    decimal Commission,
    decimal WalkerPayout);
