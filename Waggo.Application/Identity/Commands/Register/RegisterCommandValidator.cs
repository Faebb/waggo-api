using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.Register;

/// <summary>
/// Password strength for <see cref="RegisterCommand"/> (RNF-001): the shape of the email is a DTO (capa 1) concern.
/// </summary>
internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public const int MinPasswordLength = PasswordStrength.MinLength;

    public RegisterCommandValidator()
    {
        RuleFor(command => command.Password)
            .Must(PasswordStrength.IsStrong)
            .WithError(IdentityErrors.WeakPassword);
    }
}
