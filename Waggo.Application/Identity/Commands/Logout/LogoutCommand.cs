using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<bool>;
