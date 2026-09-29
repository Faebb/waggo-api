using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Common;

/// <summary>
/// Catalog entry for a business error. Define them as static members per module (e.g. <c>PricingErrors</c>)
/// and add them with <c>response.AddError(PricingErrors.InvalidDuration)</c>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation);
