using FluentValidation;
using Waggo.Api.Endpoints.Identity.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;

namespace Waggo.Api.Endpoints.Identity.Validators;

/// <summary>
/// First layer (DTO): required fields and the shape of the email. Password strength lives in
/// <c>RegisterCommandValidator</c> (Application), since it needs no I/O and is a business rule (RNF-001).
/// </summary>
internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("email"))
            .EmailAddress()
            .WithError(RequestErrors.InvalidValue("email", "Escribe un correo con el formato nombre@dominio.com."));

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithError(RequestErrors.Required("password"));
    }
}
