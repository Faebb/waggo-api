using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Application.Identity.Commands.UpdateMyProfile;

/// <summary>Perfil de usuario: the signed-in account edits its own full name and phone.</summary>
internal sealed class UpdateMyProfileHandler(
    IValidator<UpdateMyProfileCommand> validator,
    IUserRepository users,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateMyProfileCommand, UserProfileResponse>
{
    public async Task<WaggoResponse<UserProfileResponse>> HandleAsync(
        UpdateMyProfileCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<UserProfileResponse> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        User user = await users.GetAsync(Guid.Parse(currentUser.Id), cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {currentUser.Id} was not found.");

        user.UpdateProfile(command.FullName?.Trim(), command.Phone?.Trim());
        await users.SaveChangesAsync(cancellationToken);

        response.Data = UserProfileResponse.From(user);
        return response;
    }
}
