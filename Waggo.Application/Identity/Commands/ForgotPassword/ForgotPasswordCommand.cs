using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand<bool>;
