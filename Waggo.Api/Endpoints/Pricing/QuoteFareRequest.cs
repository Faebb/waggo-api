using Waggo.Application.Pricing.QuoteFare;
using Waggo.Domain.Pricing;

namespace Waggo.Api.Endpoints.Pricing;

/// <summary>Query string of <c>GET /api/v1/pricing/quote</c>. Raw values: they are validated before use.</summary>
public sealed record QuoteFareRequest(string? WalkType, int? DurationMinutes)
{
    /// <summary>Only call it after the request passed <see cref="QuoteFareRequestValidator"/>.</summary>
    public QuoteFareQuery ToQuery() =>
        new(Enum.Parse<WalkType>(WalkType!, ignoreCase: true), DurationMinutes!.Value);
}
