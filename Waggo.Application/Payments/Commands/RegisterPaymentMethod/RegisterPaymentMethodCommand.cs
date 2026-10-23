using Waggo.Application.Common.Interfaces;
using Waggo.Domain.Enums.Payments;

namespace Waggo.Application.Payments.Commands.RegisterPaymentMethod;

public sealed record RegisterPaymentMethodCommand(
    CardBrand Brand,
    string LastFour,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault) : ICommand<PaymentMethodResponse>;
