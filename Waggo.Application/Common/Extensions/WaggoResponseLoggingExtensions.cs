using Microsoft.Extensions.Logging;
using Waggo.Domain.Common;

namespace Waggo.Application.Common.Extensions;

/// <summary>
/// Writes the stacks of a <see cref="WaggoResponse{T}"/> to the log (Serilog behind <see cref="ILogger"/>):
/// errors as Error, warnings as Warning, infos as Information.
/// Call it once, where you decide (usually the endpoint).
/// </summary>
public static class WaggoResponseLoggingExtensions
{
    private const string Template = "[{Operation}] {Code}: {Message}";

    public static void WriteLogs<T>(this WaggoResponse<T> response, ILogger logger, string operation)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (WaggoMessage error in response.Errors)
        {
            logger.LogError(Template, operation, error.Code, error.Message);
        }

        foreach (WaggoMessage warning in response.Warnings)
        {
            logger.LogWarning(Template, operation, warning.Code, warning.Message);
        }

        foreach (WaggoMessage info in response.Infos)
        {
            logger.LogInformation(Template, operation, info.Code, info.Message);
        }
    }
}
