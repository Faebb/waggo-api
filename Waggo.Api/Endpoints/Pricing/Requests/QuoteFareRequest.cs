using Waggo.Application.Pricing.Queries.QuoteFare;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Api.Endpoints.Pricing.Requests;

/// <summary>Query string of <c>GET /api/v1/pricing/quote</c>. Raw values: they are validated before use.</summary>
public sealed record QuoteFareRequest(string? WalkType, int? DurationMinutes)
{
    /// <summary>Only call it after the request passed <c>QuoteFareRequestValidator</c>.</summary>
    public QuoteFareQuery ToQuery() =>
        new(Enum.Parse<WalkType>(WalkType!, ignoreCase: true), DurationMinutes!.Value);
}
