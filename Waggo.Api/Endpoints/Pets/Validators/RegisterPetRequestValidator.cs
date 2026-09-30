using FluentValidation;
using Waggo.Api.Endpoints.Pets.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Enums.Pets;

namespace Waggo.Api.Endpoints.Pets.Validators;

/// <summary>
/// First layer (DTO): required fields and formats. Business rules (lengths, weight, dates, limit per owner) live in
/// <c>RegisterPetCommandValidator</c> and the <c>Pet</c> entity.
/// </summary>
internal sealed class RegisterPetRequestValidator : AbstractValidator<RegisterPetRequest>
{
    private static readonly string s_sizes = string.Join(", ", Enum.GetNames<PetSize>());

    public RegisterPetRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithError(RequestErrors.Required("name"));

        RuleFor(request => request.Size)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("size"))
            .Must(value => Enum.TryParse<PetSize>(value, ignoreCase: true, out _) && !int.TryParse(value, out _))
            .WithError(RequestErrors.InvalidValue("size", $"Usa uno de estos valores: {s_sizes}."));
    }
}
