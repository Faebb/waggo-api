using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

/// <summary>
/// First layer (DTO): both passwords are present. The strength of the new one is a business rule (capa 2) and the
/// session's refresh token is optional.
/// </summary>
internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
            .WithError(RequestErrors.Required("currentPassword"));

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithError(RequestErrors.Required("newPassword"));
    }
}
