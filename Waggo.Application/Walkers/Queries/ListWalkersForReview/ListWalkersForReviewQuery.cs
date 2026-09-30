using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Application.Walkers.Queries.ListWalkersForReview;

/// <summary>Walker profiles for the admin; with a status, only those (e.g. the pending ones).</summary>
public sealed record ListWalkersForReviewQuery(VerificationStatus? Status)
    : IQuery<IReadOnlyList<WalkerProfileResponse>>;
