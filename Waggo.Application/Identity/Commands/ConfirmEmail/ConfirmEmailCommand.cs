using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Email, string Code) : ICommand<bool>;
