namespace Waggo.Domain.Services.Walks;

/// <summary>Rules to match open requests with walkers (RF-006).</summary>
public static class WalkMatching
{
    /// <summary>A walker only sees requests this close when sharing a position. Provisional (PO question).</summary>
    public const double MaxDistanceKm = 5;
}
