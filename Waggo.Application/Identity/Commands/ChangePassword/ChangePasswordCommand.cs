using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Identity.Commands.ChangePassword;

/// <summary>The refresh token of the session that is changing the password stays open; the others are closed.</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword, string? RefreshToken)
    : ICommand<bool>;
