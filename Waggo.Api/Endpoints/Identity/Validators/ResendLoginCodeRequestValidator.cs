using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class ResendLoginCodeRequestValidator : AbstractValidator<ResendLoginCodeRequest>
{
    public ResendLoginCodeRequestValidator()
    {
        RuleFor(request => request.ChallengeId)
            .NotEmpty()
            .WithError(RequestErrors.Required("challengeId"));
    }
}
