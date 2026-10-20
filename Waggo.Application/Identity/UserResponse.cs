using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity;

/// <summary>An account as the client sees it after registering. Shared by every Identity use case.</summary>
public sealed record UserResponse(Guid Id, string Email)
{
    public static UserResponse From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResponse(user.Id, user.Email);
    }
}
