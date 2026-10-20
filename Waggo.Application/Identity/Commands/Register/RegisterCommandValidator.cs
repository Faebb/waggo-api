using System.Text.RegularExpressions;
using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Application.Identity.Commands.Register;

/// <summary>
/// Password strength for <see cref="RegisterCommand"/> (RNF-001): the shape of the email is a DTO (capa 1) concern.
/// </summary>
internal sealed partial class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public const int MinPasswordLength = 8;

    public RegisterCommandValidator()
    {
        RuleFor(command => command.Password)
            .Must(password => password is not null
                && password.Length >= MinPasswordLength
                && StrengthRule().IsMatch(password))
            .WithError(IdentityErrors.WeakPassword);
    }

    [GeneratedRegex(@"(?=.*[A-Z])(?=.*\d)")]
    private static partial Regex StrengthRule();
}
