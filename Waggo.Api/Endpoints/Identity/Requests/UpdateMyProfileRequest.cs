using Waggo.Application.Identity.Commands.UpdateMyProfile;

namespace Waggo.Api.Endpoints.Identity.Requests;

/// <summary>
/// Body of <c>PUT /api/v1/users/me</c>. Both fields are optional (a null clears that field), so there is nothing
/// to check at the DTO layer; length is the only rule and lives in <see cref="UpdateMyProfileCommandValidator"/>.
/// </summary>
public sealed record UpdateMyProfileRequest(string? FullName, string? Phone)
{
    public UpdateMyProfileCommand ToCommand() => new(FullName, Phone);
}
