using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Queries.GetMyProfile;

/// <summary>Perfil de usuario: the signed-in account's own email, full name and phone.</summary>
internal sealed class GetMyProfileHandler(IUserRepository users, ICurrentUser currentUser)
    : IQueryHandler<GetMyProfileQuery, UserProfileResponse>
{
    public async Task<WaggoResponse<UserProfileResponse>> HandleAsync(
        GetMyProfileQuery query,
        CancellationToken cancellationToken)
    {
        User user = await users.GetAsync(Guid.Parse(currentUser.Id), cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {currentUser.Id} was not found.");
        return new WaggoResponse<UserProfileResponse> { Data = UserProfileResponse.From(user) };
    }
}
