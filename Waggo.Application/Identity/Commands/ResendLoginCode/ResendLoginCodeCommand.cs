using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ResendLoginCode;

public sealed record ResendLoginCodeCommand(Guid ChallengeId) : ICommand<LoginResponse>;
