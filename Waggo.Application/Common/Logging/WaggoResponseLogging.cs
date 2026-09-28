using Microsoft.Extensions.Logging;
using Waggo.Domain.Common;

namespace Waggo.Application.Common.Logging;

/// <summary>
/// Writes the stacks of a <see cref="WaggoResponse"/> to the log. The code that owns the response decides
/// WHERE this happens (usually the endpoint, or a use case that must log something immediately).
/// Errors → Error, Warnings → Warning, Infos → Information. Each message is written only once,
/// even if the response is concatenated and written again later.
/// </summary>
public static partial class WaggoResponseLogging
{
    public static WaggoResponse WriteLogs(this WaggoResponse response, ILogger logger, string? operation = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(logger);

        var name = operation ?? "operation";

        foreach (var message in response.Errors.Where(m => !m.IsLogged))
        {
            LogError(logger, name, message.Code, message.Message, message.Field, message.Visibility);
            message.MarkAsLogged();
        }

        foreach (var message in response.Warnings.Where(m => !m.IsLogged))
        {
            LogWarning(logger, name, message.Code, message.Message, message.Field, message.Visibility);
            message.MarkAsLogged();
        }

        foreach (var message in response.Infos.Where(m => !m.IsLogged))
        {
            LogInfo(logger, name, message.Code, message.Message, message.Field, message.Visibility);
            message.MarkAsLogged();
        }

        return response;
    }

    public static WaggoResponse<T> WriteLogs<T>(this WaggoResponse<T> response, ILogger logger, string? operation = null)
    {
        WriteLogs((WaggoResponse)response, logger, operation);
        return response;
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error,
        Message = "[{Operation}] {Code}: {Text} (field: {Field}, visibility: {Visibility})")]
    private static partial void LogError(ILogger logger, string operation, string code, string text, string? field, MessageVisibility visibility);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "[{Operation}] {Code}: {Text} (field: {Field}, visibility: {Visibility})")]
    private static partial void LogWarning(ILogger logger, string operation, string code, string text, string? field, MessageVisibility visibility);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information,
        Message = "[{Operation}] {Code}: {Text} (field: {Field}, visibility: {Visibility})")]
    private static partial void LogInfo(ILogger logger, string operation, string code, string text, string? field, MessageVisibility visibility);
}
