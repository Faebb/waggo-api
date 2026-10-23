using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.Login;

/// <summary>
/// RF-005, step 1: a right password does not grant a session yet — it only raises the second factor (a code
/// emailed to the account). Wrong attempts are counted and lock the account for a while (RNF-002 brute force).
/// </summary>
internal sealed class LoginHandler(
    IUserRepository users,
    ILoginChallengeRepository challenges,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<WaggoResponse<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<LoginResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        string normalizedEmail = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        User? user = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            response.AddError(IdentityErrors.InvalidCredentials);
            return response;
        }

        if (user.IsLocked(now))
        {
            response.AddError(IdentityErrors.TooManyAttempts);
            return response;
        }

        if (!passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            user.RegisterFailedLoginAttempt(now);
            await users.SaveChangesAsync(cancellationToken);
            response.AddError(IdentityErrors.InvalidCredentials);
            return response;
        }

        if (!user.EmailConfirmed)
        {
            response.AddError(IdentityErrors.AccountNotConfirmed);
            return response;
        }

        user.ResetFailedLoginAttempts();
        await users.SaveChangesAsync(cancellationToken);

        string code = tokenGenerator.GenerateNumericCode();
        LoginChallenge challenge = LoginChallenge.Create(user.Id, tokenGenerator.Hash(code), now);
        await challenges.AddAsync(challenge, cancellationToken);
        await emailSender.SendAsync(
            user.Email,
            LoginCodeEmail.Subject,
            LoginCodeEmail.Body(code),
            cancellationToken);

        response.Data = new LoginResponse(challenge.Id);
        return response;
    }
}
