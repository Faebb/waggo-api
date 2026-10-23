using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Errors.Payments;

public static class PaymentErrors
{
    public static readonly Error Declined = new(
        "Payments.Declined",
        "Tu método de pago fue rechazado. Revísalo o usa otro e intenta de nuevo.",
        ErrorType.BusinessRule);

    public static readonly Error NotHeld = new(
        "Payments.NotHeld",
        "Este pago ya fue cobrado o liberado.",
        ErrorType.BusinessRule);

    public static readonly Error NotFound = new(
        "Payments.NotFound",
        "Este paseo no tiene un pago registrado.",
        ErrorType.NotFound);

    public static readonly Error GatewayUnavailable = new(
        "Payments.GatewayUnavailable",
        "No pudimos comunicarnos con la pasarela de pagos. Intenta de nuevo en unos minutos.",
        ErrorType.ServiceUnavailable);

    public static readonly Error FullCardNumberNotAllowed = new(
        "Payments.FullCardNumberNotAllowed",
        "No envíes el número completo de la tarjeta. Solo necesitamos la marca, los últimos 4 dígitos y el vencimiento.");

    public static readonly Error InvalidLastFour = new(
        "Payments.InvalidLastFour",
        "Escribe solo los últimos 4 dígitos de la tarjeta, por ejemplo 4242.");

    public static readonly Error InvalidExpiry = new(
        "Payments.InvalidExpiry",
        "Revisa el vencimiento: el mes va de 1 a 12 y el año puede estar hasta 20 años adelante.");

    public static readonly Error CardExpired = new(
        "Payments.CardExpired",
        "Esta tarjeta ya venció. Registra una tarjeta vigente.",
        ErrorType.BusinessRule);
}
