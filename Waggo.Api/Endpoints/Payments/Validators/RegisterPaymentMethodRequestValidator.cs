using FluentValidation;
using Waggo.Api.Endpoints.Payments.Requests;
using Waggo.Api.Infrastructure.Errors;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Api.Endpoints.Payments.Validators;

/// <summary>
/// First layer (DTO): required fields, allowed brands and the rule that no full card number may be sent. Business
/// rules (last four digits, expiry) live in <c>RegisterPaymentMethodCommandValidator</c> and the entity.
/// </summary>
internal sealed class RegisterPaymentMethodRequestValidator : AbstractValidator<RegisterPaymentMethodRequest>
{
    private static readonly string s_brands = string.Join(", ", Enum.GetNames<CardBrand>());

    public RegisterPaymentMethodRequestValidator()
    {
        RuleFor(request => request.CardNumber)
            .Must(string.IsNullOrWhiteSpace)
            .WithError(PaymentErrors.FullCardNumberNotAllowed);

        RuleFor(request => request.Number)
            .Must(string.IsNullOrWhiteSpace)
            .WithError(PaymentErrors.FullCardNumberNotAllowed);

        RuleFor(request => request.Brand)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithError(RequestErrors.Required("brand"))
            .Must(value => Enum.TryParse<CardBrand>(value, ignoreCase: true, out _) && !int.TryParse(value, out _))
            .WithError(RequestErrors.InvalidValue("brand", $"Usa uno de estos valores: {s_brands}."));

        RuleFor(request => request.LastFour)
            .NotEmpty()
            .WithError(RequestErrors.Required("lastFour"));

        RuleFor(request => request.ExpiryMonth)
            .NotNull()
            .WithError(RequestErrors.Required("expiryMonth"));

        RuleFor(request => request.ExpiryYear)
            .NotNull()
            .WithError(RequestErrors.Required("expiryYear"));
    }
}
