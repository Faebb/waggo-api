namespace Waggo.Domain.Common;

/// <summary>
/// Catalog entry for a business error. Define them as static members per module (e.g. <c>PricingErrors</c>)
/// and add them with <c>response.AddError(PricingErrors.InvalidDuration)</c>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation);

/// <summary>Kind of error. The API maps it to the HTTP status code.</summary>
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,

    /// <summary>A business rule prevents the operation (e.g. the walker is not verified yet).</summary>
    BusinessRule,

    /// <summary>An external service (payments, maps, identity) is not available.</summary>
    ServiceUnavailable,
    Unexpected,
}
