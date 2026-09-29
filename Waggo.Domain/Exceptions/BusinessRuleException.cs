using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Exceptions;

/// <summary>A business rule prevents the operation (422).</summary>
public sealed class BusinessRuleException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.BusinessRule, detail, innerException);
