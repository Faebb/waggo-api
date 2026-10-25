using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<UserProfileResponse>;
