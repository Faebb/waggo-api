using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.VerifyLoginCode;

public sealed record VerifyLoginCodeCommand(Guid ChallengeId, string Code) : ICommand<SessionResponse>;
