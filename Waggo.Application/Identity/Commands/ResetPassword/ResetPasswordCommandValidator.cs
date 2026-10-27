using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.ResetPassword;

/// <summary>Strength of the new password for <see cref="ResetPasswordCommand"/> (RNF-001).</summary>
internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.NewPassword)
            .Must(PasswordStrength.IsStrong)
            .WithError(IdentityErrors.WeakPassword);
    }
}
