using System.Text.RegularExpressions;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Walkers;
using Waggo.Domain.Errors.Walkers;

namespace Waggo.Domain.Entities.Walkers;

/// <summary>
/// A walker's profile (RF-002) and its verification (RF-003). The document number is encrypted at rest (RNF-003) and
/// only its last 4 digits are shown.
/// </summary>
public sealed partial class WalkerProfile
{
    public const int MinFullNameLength = 3;
    public const int MaxFullNameLength = 100;
    public const int MaxExperienceLength = 500;
    public const int MinRejectionReasonLength = 5;
    public const int MaxRejectionReasonLength = 300;

    // Used by EF Core to materialize the entity.
    private WalkerProfile()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Id of the walker in the identity provider (<c>sub</c> claim).</summary>
    public string UserId { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public DocumentType DocumentType { get; private set; }

    public string DocumentNumber { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Experience { get; private set; }

    public VerificationStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public string? ReviewedBy { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string DocumentLast4 => DocumentNumber.Length <= 4 ? DocumentNumber : DocumentNumber[^4..];

    /// <summary>Only verified walkers see requests and accept walks.</summary>
    public bool IsVerified => Status == VerificationStatus.Approved;

    public static WaggoResponse<WalkerProfile> Register(
        string userId,
        string fullName,
        DocumentType documentType,
        string documentNumber,
        string phone,
        string? experience,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        WaggoResponse<WalkerProfile> response = new();

        string name = fullName?.Trim() ?? string.Empty;
        string document = documentNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        string phoneNumber = phone?.Trim() ?? string.Empty;
        string? about = string.IsNullOrWhiteSpace(experience) ? null : experience.Trim();

        if (name.Length is < MinFullNameLength or > MaxFullNameLength)
        {
            response.AddError(WalkerErrors.InvalidFullName);
        }

        Regex documentRule = documentType == DocumentType.PP ? PassportRule() : NumericDocumentRule();
        if (!documentRule.IsMatch(document))
        {
            response.AddError(WalkerErrors.InvalidDocument);
        }

        if (!PhoneRule().IsMatch(phoneNumber))
        {
            response.AddError(WalkerErrors.InvalidPhone);
        }

        if (about?.Length > MaxExperienceLength)
        {
            response.AddError(WalkerErrors.InvalidExperience);
        }

        if (!response.IsValid)
        {
            return response;
        }

        response.Data = new WalkerProfile
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            FullName = name,
            DocumentType = documentType,
            DocumentNumber = document,
            Phone = phoneNumber,
            Experience = about,
            Status = VerificationStatus.Pending,
            RegisteredAt = now,
        };
        return response;
    }

    /// <summary>An admin (or, later, the verification provider) approves the walker.</summary>
    public WaggoResponse<WalkerProfile> Approve(string reviewerId, DateTimeOffset now)
    {
        WaggoResponse<WalkerProfile> response = new();
        if (Status != VerificationStatus.Pending)
        {
            response.AddError(WalkerErrors.NotPending);
            return response;
        }

        Status = VerificationStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        response.Data = this;
        return response;
    }

    /// <summary>An admin rejects the walker, saying why so they know what to fix.</summary>
    public WaggoResponse<WalkerProfile> Reject(string reviewerId, string reason, DateTimeOffset now)
    {
        WaggoResponse<WalkerProfile> response = new();
        string why = reason?.Trim() ?? string.Empty;

        if (why.Length is < MinRejectionReasonLength or > MaxRejectionReasonLength)
        {
            response.AddError(WalkerErrors.InvalidRejectionReason);
            return response;
        }

        if (Status != VerificationStatus.Pending)
        {
            response.AddError(WalkerErrors.NotPending);
            return response;
        }

        Status = VerificationStatus.Rejected;
        RejectionReason = why;
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        response.Data = this;
        return response;
    }

    [GeneratedRegex("^[0-9]{5,15}$")]
    private static partial Regex NumericDocumentRule();

    [GeneratedRegex("^[A-Z0-9]{5,15}$")]
    private static partial Regex PassportRule();

    [GeneratedRegex("^[0-9]{10}$")]
    private static partial Regex PhoneRule();
}
