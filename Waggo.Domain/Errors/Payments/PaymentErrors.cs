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
}
