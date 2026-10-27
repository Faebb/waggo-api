using System.Text.RegularExpressions;
using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

/// <summary>First layer (DTO): the code is present and shaped like the 6 digits of the email.</summary>
internal sealed partial class ConfirmEmailChangeRequestValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeRequestValidator()
    {
        RuleFor(request => request.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("code"))
            .Must(code => code is not null && SixDigits().IsMatch(code))
            .WithError(RequestErrors.InvalidValue("code", "El código tiene 6 números."));
    }

    [GeneratedRegex(@"\A[0-9]{6}\z")]
    private static partial Regex SixDigits();
}
