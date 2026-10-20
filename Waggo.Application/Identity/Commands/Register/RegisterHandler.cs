using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.Register;

/// <summary>RF-001: anyone creates an account; it stays inactive until the confirmation email is answered.</summary>
internal sealed class RegisterHandler(
    IValidator<RegisterCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterCommand, UserResponse>
{
    public async Task<WaggoResponse<UserResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<UserResponse> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            response.AddError(IdentityErrors.EmailAlreadyRegistered);
            return response;
        }

        string passwordHash = passwordHasher.Hash(command.Password);
        string confirmationToken = tokenGenerator.GenerateToken();
        string confirmationTokenHash = tokenGenerator.Hash(confirmationToken);

        WaggoResponse<User> user =
            User.Register(normalizedEmail, passwordHash, confirmationTokenHash, timeProvider.GetUtcNow());
        response.ConcatStacks(user);
        if (!response.IsValid)
        {
            return response;
        }

        await users.AddAsync(user.Data, cancellationToken);
        await emailSender.SendAsync(
            user.Data.Email,
            "Confirma tu cuenta de Waggo",
            $"Confirma tu cuenta con este código: {confirmationToken}",
            cancellationToken);

        response.Data = UserResponse.From(user.Data);
        return response;
    }
}
