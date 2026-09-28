namespace Waggo.Domain.Common;

/// <summary>Business error with a stable code (used by clients) and a human-readable message.</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}
