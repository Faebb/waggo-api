using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Exceptions;

/// <summary>The user is not authenticated or the session is invalid (401).</summary>
public sealed class UnauthorizedException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.Unauthorized, detail, innerException);
