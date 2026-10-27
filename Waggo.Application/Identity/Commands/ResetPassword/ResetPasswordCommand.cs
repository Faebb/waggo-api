using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : ICommand<bool>;
