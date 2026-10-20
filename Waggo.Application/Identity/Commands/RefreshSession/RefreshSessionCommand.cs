using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<SessionResponse>;
