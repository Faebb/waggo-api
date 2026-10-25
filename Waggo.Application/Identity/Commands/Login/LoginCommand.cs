using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<SessionResponse>;
