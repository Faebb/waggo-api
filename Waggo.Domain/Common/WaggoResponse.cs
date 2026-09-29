namespace Waggo.Domain.Common;

/// <summary>
/// Internal response used by EVERY operation in Waggo (domain, use cases, adapters).
/// It carries three stacks — <see cref="Errors"/>, <see cref="Warnings"/> and <see cref="Infos"/> — that the
/// operation fills instead of logging. The caller decides where the stacks are written to the log
/// (see <c>WaggoResponseLogging.WriteLogs</c>) and uses <see cref="ConcatStacks"/> to carry the messages
/// of the operations it calls. It stops being valid (<see cref="IsValid"/>) as soon as it holds one error.
/// </summary>
public class WaggoResponse
{
    private readonly List<WaggoMessage> _errors = [];
    private readonly List<WaggoMessage> _warnings = [];
    private readonly List<WaggoMessage> _infos = [];

    public IReadOnlyList<WaggoMessage> Errors => _errors;

    public IReadOnlyList<WaggoMessage> Warnings => _warnings;

    public IReadOnlyList<WaggoMessage> Infos => _infos;

    /// <summary>True while the response has no errors. Warnings and infos do not affect it.</summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Kind of the first error (drives the HTTP status). <see cref="ErrorType.None"/> while valid.
    /// </summary>
    public ErrorType ErrorType => _errors.Count == 0 ? ErrorType.None : _errors[0].ErrorType;

    /// <summary>Valid response that carries <paramref name="value"/>.</summary>
    public static WaggoResponse<T> FromValue<T>(T value) => new WaggoResponse<T>().SetValue(value);

    /// <summary>Invalid response with one error.</summary>
    public static WaggoResponse FromError(Error error) => new WaggoResponse().AddError(error);

    /// <summary>Invalid response of type <typeparamref name="T"/> with one error.</summary>
    public static WaggoResponse<T> FromError<T>(Error error) => new WaggoResponse<T>().AddError(error);

    public WaggoResponse AddError(Error error, string? field = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(new WaggoMessage(error.Code, error.Message, MessageVisibility.Public, field, error.Type));
        return this;
    }

    public WaggoResponse AddError(
        string code,
        string message,
        ErrorType type = ErrorType.Validation,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        _errors.Add(new WaggoMessage(code, message, visibility, field, type));
        return this;
    }

    public WaggoResponse AddWarning(
        string code,
        string message,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        _warnings.Add(new WaggoMessage(code, message, visibility, field));
        return this;
    }

    public WaggoResponse AddInfo(
        string code,
        string message,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        _infos.Add(new WaggoMessage(code, message, visibility, field));
        return this;
    }

    /// <summary>
    /// Appends the errors, warnings and infos of <paramref name="other"/> to this response, keeping their order
    /// and their "already logged" state. Use it every time you call another operation that returns a WaggoResponse.
    /// </summary>
    public WaggoResponse ConcatStacks(WaggoResponse other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other))
        {
            return this;
        }

        _errors.AddRange(other._errors);
        _warnings.AddRange(other._warnings);
        _infos.AddRange(other._infos);
        return this;
    }

    /// <summary>Creates a response of another type that carries all the stacks of this one (no value).</summary>
    public WaggoResponse<TOut> ToResponse<TOut>() => new WaggoResponse<TOut>().ConcatStacks(this);

    public bool HasError(string code) => _errors.Exists(e => e.Code == code);

    public bool HasWarning(string code) => _warnings.Exists(w => w.Code == code);

    public bool HasInfo(string code) => _infos.Exists(i => i.Code == code);

    /// <summary>All messages that still have to be written to the log.</summary>
    public IEnumerable<WaggoMessage> PendingLogMessages() =>
        _errors.Concat(_warnings).Concat(_infos).Where(m => !m.IsLogged);
}

/// <summary><see cref="WaggoResponse"/> that also carries a value when the operation succeeds.</summary>
public sealed class WaggoResponse<T> : WaggoResponse
{
    private T? _value;

    public bool HasValue { get; private set; }

    /// <summary>
    /// The value. Throws if the response failed or no value was set;
    /// check <see cref="WaggoResponse.IsValid"/> first.
    /// </summary>
    public T Value => IsValid && HasValue
        ? _value!
        : throw new InvalidOperationException(!IsValid
            ? $"Cannot read the value of a failed response ({Errors[0]})."
            : "The response has no value.");

    /// <summary>The value or <c>default</c>; used when mapping to the API contract.</summary>
    public T? ValueOrDefault => IsValid && HasValue ? _value : default;

    public static implicit operator WaggoResponse<T>(T value) => new WaggoResponse<T>().SetValue(value);

    public static implicit operator WaggoResponse<T>(Error error) => new WaggoResponse<T>().AddError(error);

    public WaggoResponse<T> SetValue(T value)
    {
        _value = value;
        HasValue = true;
        return this;
    }

    public new WaggoResponse<T> AddError(Error error, string? field = null)
    {
        base.AddError(error, field);
        return this;
    }

    public new WaggoResponse<T> AddError(
        string code,
        string message,
        ErrorType type = ErrorType.Validation,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        base.AddError(code, message, type, visibility, field);
        return this;
    }

    public new WaggoResponse<T> AddWarning(
        string code,
        string message,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        base.AddWarning(code, message, visibility, field);
        return this;
    }

    public new WaggoResponse<T> AddInfo(
        string code,
        string message,
        MessageVisibility visibility = MessageVisibility.Public,
        string? field = null)
    {
        base.AddInfo(code, message, visibility, field);
        return this;
    }

    public new WaggoResponse<T> ConcatStacks(WaggoResponse other)
    {
        base.ConcatStacks(other);
        return this;
    }

    /// <summary>Transforms the value (only when successful) and keeps every stack.</summary>
    public WaggoResponse<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        WaggoResponse<TOut> result = ToResponse<TOut>();
        return IsValid && HasValue ? result.SetValue(map(_value!)) : result;
    }
}
