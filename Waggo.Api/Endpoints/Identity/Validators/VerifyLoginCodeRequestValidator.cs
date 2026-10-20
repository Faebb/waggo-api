using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

internal sealed class VerifyLoginCodeRequestValidator : AbstractValidator<VerifyLoginCodeRequest>
{
    public VerifyLoginCodeRequestValidator()
    {
        RuleFor(request => request.ChallengeId)
            .NotEmpty()
            .WithError(RequestErrors.Required("challengeId"));

        RuleFor(request => request.Code)
            .NotEmpty()
            .WithError(RequestErrors.Required("code"));
    }
}
