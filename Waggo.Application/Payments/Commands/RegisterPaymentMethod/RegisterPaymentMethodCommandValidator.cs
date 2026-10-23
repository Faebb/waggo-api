using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Entities.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Application.Payments.Commands.RegisterPaymentMethod;

/// <summary>
/// Field rules of <see cref="RegisterPaymentMethodCommand"/> (RF-020). The <see cref="PaymentMethod"/> entity repeats
/// them and adds the clock rules (expired card, year limit).
/// </summary>
internal sealed class RegisterPaymentMethodCommandValidator : AbstractValidator<RegisterPaymentMethodCommand>
{
    public RegisterPaymentMethodCommandValidator()
    {
        RuleFor(command => command.LastFour)
            .Must(lastFour => !PaymentMethod.IsFullCardNumber(lastFour))
            .WithError(PaymentErrors.FullCardNumberNotAllowed);

        RuleFor(command => command.LastFour)
            .Must(PaymentMethod.IsValidLastFour)
            .WithError(PaymentErrors.InvalidLastFour);

        RuleFor(command => command.ExpiryMonth)
            .Must(month => month is >= 1 and <= 12)
            .WithError(PaymentErrors.InvalidExpiry);
    }
}
