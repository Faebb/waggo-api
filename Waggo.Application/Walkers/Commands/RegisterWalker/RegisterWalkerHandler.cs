using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Errors.Walkers;

namespace Waggo.Application.Walkers.Commands.RegisterWalker;

/// <summary>RF-002: the current user becomes a walker pending verification.</summary>
internal sealed class RegisterWalkerHandler(
    IWalkerProfileRepository profiles,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterWalkerCommand, WalkerProfileResponse>
{
    public async Task<WaggoResponse<WalkerProfileResponse>> HandleAsync(
        RegisterWalkerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkerProfileResponse> response = new();

        if (await profiles.GetByUserAsync(currentUser.Id, cancellationToken) is not null)
        {
            response.AddError(WalkerErrors.AlreadyRegistered);
            return response;
        }

        WaggoResponse<WalkerProfile> profile = WalkerProfile.Register(
            currentUser.Id,
            command.FullName,
            command.DocumentType,
            command.DocumentNumber,
            command.Phone,
            command.Experience,
            timeProvider.GetUtcNow());
        response.ConcatStacks(profile);
        if (!response.IsValid)
        {
            return response;
        }

        await profiles.AddAsync(profile.Data, cancellationToken);
        response.Data = WalkerProfileResponse.From(profile.Data);
        return response;
    }
}
