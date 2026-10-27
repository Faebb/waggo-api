using System.Text.RegularExpressions;
using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

/// <summary>
/// First layer (DTO): the three fields are present and the code is shaped like the 6 digits of the email. The
/// strength of the new password is a business rule (capa 2).
/// </summary>
internal sealed partial class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithError(RequestErrors.Required("email"));

        RuleFor(request => request.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("code"))
            .Must(code => code is not null && SixDigits().IsMatch(code))
            .WithError(RequestErrors.InvalidValue("code", "El código tiene 6 números."));

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithError(RequestErrors.Required("newPassword"));
    }

    [GeneratedRegex(@"\A[0-9]{6}\z")]
    private static partial Regex SixDigits();
}
