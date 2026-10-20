using System.Security.Claims;
using Waggo.Application.Common.Interfaces;

namespace Waggo.Api.Infrastructure.Authentication;

/// <summary>Reads the current user from the request: <c>sub</c> of the JWT, or the development user's id.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Id
    {
        get
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue("sub")
                ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("The current request has no authenticated user.");
        }
    }
}
