using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .WithError(RequestErrors.Required("refreshToken"));
    }
}
