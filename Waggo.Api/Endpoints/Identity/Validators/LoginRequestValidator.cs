using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithError(RequestErrors.Required("email"));

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithError(RequestErrors.Required("password"));
    }
}
