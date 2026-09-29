namespace Waggo.Domain.Common;

/// <summary>
/// Catalog entry for a business error: stable code (clients depend on it), message and kind.
/// Define them as static members per module (e.g. <c>PricingErrors</c>) and add them with
/// <see cref="WaggoResponse.AddError(Error, string?)"/>.
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
    Unexpected,
}
