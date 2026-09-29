namespace Waggo.Domain.Common.Exceptions;

/// <summary>The user is authenticated but cannot perform the operation (403).</summary>
public sealed class ForbiddenException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.Forbidden, detail, innerException);
