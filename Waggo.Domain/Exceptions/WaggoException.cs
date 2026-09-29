using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.Exceptions;

/// <summary>
/// Base of the custom exceptions. Throw one when an operation cannot continue (not found, forbidden, conflict,
/// broken business rule, external service down). The API middleware turns it into the right HTTP status and a
/// WaggoApiResponse with <see cref="Error"/> (public code + Spanish message). <see cref="Exception.Message"/> is
/// technical detail for the log and never reaches the user.
/// Expected validation problems are not exceptions: they are errors in the WaggoResponse.
/// </summary>
public abstract class WaggoException : Exception
{
    protected WaggoException(Error error, ErrorType errorType, string? detail, Exception? innerException)
        : base(detail ?? error?.Message, innerException)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
        ErrorType = errorType;
    }

    /// <summary>What the user sees: stable code and a Spanish message that explains how to solve it.</summary>
    public Error Error { get; }

    /// <summary>Drives the HTTP status code.</summary>
    public ErrorType ErrorType { get; }
}
