using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(request => request.IdToken)
            .NotEmpty()
            .WithError(RequestErrors.Required("idToken"));
    }
}
