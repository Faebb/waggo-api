namespace Waggo.Domain.Enums.Walkers;

/// <summary>Where a walker is in the verification (RF-003). Only approved walkers get walks.</summary>
public enum VerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}
