using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>The signed-in user's own account, for the profile screen (RF-001-RF-005 perfil de usuario).</summary>
public sealed record UserProfileResponse(Guid Id, string Email, string? FullName, string? Phone)
{
    public static UserProfileResponse From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserProfileResponse(user.Id, user.Email, user.FullName, user.Phone);
    }
}
