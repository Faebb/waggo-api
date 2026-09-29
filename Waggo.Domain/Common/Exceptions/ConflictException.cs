namespace Waggo.Domain.Common.Exceptions;

/// <summary>The operation conflicts with the current state of the resource (409).</summary>
public sealed class ConflictException(Error error, string? detail = null, Exception? innerException = null)
    : WaggoException(error, ErrorType.Conflict, detail, innerException);
