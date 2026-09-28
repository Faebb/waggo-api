using Waggo.Domain.Pricing;

namespace Waggo.Application.Pricing;

/// <summary>Port: where the current pricing table comes from (configuration today, database tomorrow).</summary>
public interface IPricingTableProvider
{
    Task<PricingTable> GetCurrentAsync(CancellationToken cancellationToken);
}
