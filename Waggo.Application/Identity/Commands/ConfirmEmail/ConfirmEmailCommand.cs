using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : ICommand<bool>;
