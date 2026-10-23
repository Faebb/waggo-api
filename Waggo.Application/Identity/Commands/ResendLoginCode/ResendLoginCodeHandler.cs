using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity.Commands.Login;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ResendLoginCode;

/// <summary>
/// RF-005: sends a new second-factor code for a pending login, by the same channel (email). The old code stops
/// working and the resend counts against the challenge's attempt limit. Only the challenge id is received, so the
/// response never reveals whether an email has an account.
/// </summary>
internal sealed class ResendLoginCodeHandler(
    ILoginChallengeRepository challenges,
    IUserRepository users,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<ResendLoginCodeCommand, LoginResponse>
{
    public async Task<WaggoResponse<LoginResponse>> HandleAsync(
        ResendLoginCodeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<LoginResponse> response = new();
        DateTimeOffset now = timeProvider.GetUtcNow();

        LoginChallenge? challenge = await challenges.GetAsync(command.ChallengeId, cancellationToken);
        User? user = challenge is null ? null : await users.GetAsync(challenge.UserId, cancellationToken);
        if (challenge is null || user is null)
        {
            response.AddError(IdentityErrors.ChallengeNotFound);
            return response;
        }

        if (user.IsLocked(now))
        {
            response.AddError(IdentityErrors.TooManyAttempts);
            return response;
        }

        string code = tokenGenerator.GenerateNumericCode();
        response.ConcatStacks(challenge.ReplaceCode(tokenGenerator.Hash(code), now));
        await challenges.SaveChangesAsync(cancellationToken);
        if (!response.IsValid)
        {
            return response;
        }

        await emailSender.SendAsync(user.Email, LoginCodeEmail.Subject, LoginCodeEmail.Body(code), cancellationToken);

        response.Data = new LoginResponse(challenge.Id);
        return response;
    }
}
