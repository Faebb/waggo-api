using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.RequestEmailChange;

public sealed record RequestEmailChangeCommand(string NewEmail, string Password) : ICommand<bool>;
