using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

/// <summary>First layer (DTO): the new email is present and shaped like one, and the password is present.</summary>
internal sealed class RequestEmailChangeRequestValidator : AbstractValidator<RequestEmailChangeRequest>
{
    public RequestEmailChangeRequestValidator()
    {
        RuleFor(request => request.NewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("newEmail"))
            .EmailAddress()
            .WithError(RequestErrors.InvalidValue("newEmail", "Escribe un correo con el formato nombre@dominio.com."));

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithError(RequestErrors.Required("password"));
    }
}
