using FluentValidation;
using Waggo.Api.Endpoints.Walkers.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Api.Endpoints.Walkers.Validators;

/// <summary>First layer (DTO): required fields and the document type. Formats are domain rules.</summary>
internal sealed class RegisterWalkerRequestValidator : AbstractValidator<RegisterWalkerRequest>
{
    private static readonly string s_documentTypes = string.Join(", ", Enum.GetNames<DocumentType>());

    public RegisterWalkerRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().WithError(RequestErrors.Required("fullName"));

        RuleFor(request => request.DocumentType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("documentType"))
            .Must(value => Enum.TryParse<DocumentType>(value, ignoreCase: true, out _) && !int.TryParse(value, out _))
            .WithError(RequestErrors.InvalidValue("documentType", $"Usa uno de estos valores: {s_documentTypes}."));

        RuleFor(request => request.DocumentNumber).NotEmpty().WithError(RequestErrors.Required("documentNumber"));
        RuleFor(request => request.Phone).NotEmpty().WithError(RequestErrors.Required("phone"));
    }
}
