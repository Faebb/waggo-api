using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(string? FullName, string? Phone) : ICommand<UserProfileResponse>;
