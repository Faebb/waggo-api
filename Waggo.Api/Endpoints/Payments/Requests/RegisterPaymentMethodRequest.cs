using Waggo.Application.Payments.Commands.RegisterPaymentMethod;
using Waggo.Domain.Enums.Payments;

namespace Waggo.Api.Endpoints.Payments.Requests;

/// <summary>
/// Body of <c>POST /api/v1/payments/methods</c>. Raw values: they are validated before use. <c>CardNumber</c> and
/// <c>Number</c> exist only to be rejected (RNF-004).
/// </summary>
public sealed record RegisterPaymentMethodRequest(
    string? Brand,
    string? LastFour,
    int? ExpiryMonth,
    int? ExpiryYear,
    bool? IsDefault,
    string? CardNumber,
    string? Number)
{
    /// <summary>Only call it after the request passed <c>RegisterPaymentMethodRequestValidator</c>.</summary>
    public RegisterPaymentMethodCommand ToCommand() =>
        new(Enum.Parse<CardBrand>(Brand!, ignoreCase: true), LastFour!, ExpiryMonth!.Value, ExpiryYear!.Value, IsDefault ?? false);
}
