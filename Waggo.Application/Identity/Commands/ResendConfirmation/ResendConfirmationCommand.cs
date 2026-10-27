using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ResendConfirmation;

public sealed record ResendConfirmationCommand(string Email) : ICommand<bool>;
