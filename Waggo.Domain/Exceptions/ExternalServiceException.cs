using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Exceptions;

/// <summary>An external service needed by the operation is not available (503).</summary>
public sealed class ExternalServiceException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.ServiceUnavailable, detail, innerException);
