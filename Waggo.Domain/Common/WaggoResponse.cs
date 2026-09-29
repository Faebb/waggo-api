namespace Waggo.Domain.Common;

/// <summary>
/// Response returned by every operation in Waggo (domain, use cases, adapters).
/// Instead of logging, an operation adds messages to its three stacks (errors, warnings, infos)
/// and the caller decides where they are written to the log.
/// </summary>
public sealed class WaggoResponse<T>
{
    /// <summary>Result of the operation. Only meaningful when <see cref="IsValid"/> is true.</summary>
    public T Data { get; set; } = default!;

    public List<WaggoMessage> Errors { get; } = [];

    public List<WaggoMessage> Warnings { get; } = [];

    public List<WaggoMessage> Infos { get; } = [];

    /// <summary>True while there are no errors. Warnings and infos do not affect it.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Kind of the first error; the API uses it to choose the HTTP status code.</summary>
    public ErrorType ErrorType { get; private set; } = ErrorType.None;

    public void AddError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        AddError(error.Code, error.Message, error.Type);
    }

    public void AddError(string code, string message, ErrorType type = ErrorType.Validation)
    {
        Errors.Add(new WaggoMessage(code, message));
        if (ErrorType == ErrorType.None)
        {
            ErrorType = type;
        }
    }

    public void AddWarning(string code, string message, MessageVisibility visibility = MessageVisibility.Public) =>
        Warnings.Add(new WaggoMessage(code, message, visibility));

    public void AddInfo(string code, string message, MessageVisibility visibility = MessageVisibility.Public) =>
        Infos.Add(new WaggoMessage(code, message, visibility));

    /// <summary>Passes every stack (errors, warnings and infos) of <paramref name="other"/> to this response.</summary>
    public void ConcatStacks<TOther>(WaggoResponse<TOther> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        Infos.AddRange(other.Infos);

        if (ErrorType == ErrorType.None)
        {
            ErrorType = other.ErrorType;
        }
    }
}
