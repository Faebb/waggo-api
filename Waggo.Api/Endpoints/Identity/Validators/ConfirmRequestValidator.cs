using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class ConfirmRequestValidator : AbstractValidator<ConfirmRequest>
{
    public ConfirmRequestValidator()
    {
        RuleFor(request => request.Token)
            .NotEmpty()
            .WithError(RequestErrors.Required("token"));
    }
}
