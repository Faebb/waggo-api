using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Errors.Walkers;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walkers.Queries.GetMyWalkerProfile;

/// <summary>RF-002/RF-003: the walker sees their profile and where the verification is.</summary>
internal sealed class GetMyWalkerProfileHandler(IWalkerProfileRepository profiles, ICurrentUser currentUser)
    : IQueryHandler<GetMyWalkerProfileQuery, WalkerProfileResponse>
{
    public async Task<WaggoResponse<WalkerProfileResponse>> HandleAsync(
        GetMyWalkerProfileQuery query,
        CancellationToken cancellationToken)
    {
        WalkerProfile profile = await profiles.GetByUserAsync(currentUser.Id, cancellationToken)
            ?? throw new NotFoundException(WalkerErrors.NotFound, $"{currentUser.Id} has no walker profile");
        return new WaggoResponse<WalkerProfileResponse> { Data = WalkerProfileResponse.From(profile) };
    }
}
