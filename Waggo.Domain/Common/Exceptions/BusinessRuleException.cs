namespace Waggo.Domain.Common.Exceptions;

/// <summary>A business rule prevents the operation (422).</summary>
public sealed class BusinessRuleException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.BusinessRule, detail, innerException);
