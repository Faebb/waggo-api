using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ChangePassword;

/// <summary>Strength of the new password for <see cref="ChangePasswordCommand"/> (RNF-001).</summary>
internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.NewPassword)
            .Must(PasswordStrength.IsStrong)
            .WithError(IdentityErrors.WeakPassword);
    }
}
