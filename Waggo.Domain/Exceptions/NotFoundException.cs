using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Exceptions;

/// <summary>The requested resource does not exist (404).</summary>
public sealed class NotFoundException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.NotFound, detail, innerException);
