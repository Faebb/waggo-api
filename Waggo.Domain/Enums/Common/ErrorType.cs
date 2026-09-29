namespace Waggo.Domain.Enums.Common;

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
