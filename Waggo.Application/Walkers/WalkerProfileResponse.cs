using Waggo.Domain.Entities.Walkers;

namespace Waggo.Application.Walkers;

/// <summary>A walker's profile. The document number never leaves the API: only its last 4 digits.</summary>
public sealed record WalkerProfileResponse(
    Guid Id,
    string FullName,
    string DocumentType,
    string DocumentLast4,
    string Phone,
    string? Experience,
    string Status,
    string? RejectionReason,
    DateTimeOffset RegisteredAt)
{
    public static WalkerProfileResponse From(WalkerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new WalkerProfileResponse(
            profile.Id,
            profile.FullName,
            profile.DocumentType.ToString(),
            profile.DocumentLast4,
            profile.Phone,
            profile.Experience,
            profile.Status.ToString(),
            profile.RejectionReason,
            profile.RegisteredAt);
    }
}
