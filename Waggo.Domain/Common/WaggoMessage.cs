namespace Waggo.Domain.Common;

/// <summary>One entry of the error, warning or info stack of a <see cref="WaggoResponse"/>.</summary>
public sealed class WaggoMessage
{
    public WaggoMessage(
        string code,
        string message,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null,
        ErrorType errorType = ErrorType.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Visibility = visibility;
        Field = field;
        ErrorType = errorType;
    }

    /// <summary>Stable identifier, e.g. <c>Pricing.InvalidDuration</c>.</summary>
    public string Code { get; }

    public string Message { get; }

    public MessageVisibility Visibility { get; }

    /// <summary>Input field the message refers to, when it applies.</summary>
    public string? Field { get; }

    /// <summary>Only meaningful for errors; <see cref="ErrorType.None"/> for warnings and infos.</summary>
    public ErrorType ErrorType { get; }

    /// <summary>True once the message was written to the log, so it is never written twice.</summary>
    public bool IsLogged { get; private set; }

    public void MarkAsLogged() => IsLogged = true;

    public override string ToString() => $"{Code}: {Message}";
}
