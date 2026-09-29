using Waggo.Domain.Common;
using Waggo.Domain.ValueObjects.Pricing;

namespace Waggo.Application.Common.Interfaces.Pricing;

/// <summary>Port: where the current pricing table comes from (configuration today, database tomorrow).</summary>
public interface IPricingTableProvider
{
    Task<WaggoResponse<PricingTable>> GetCurrentAsync(CancellationToken cancellationToken);
}
