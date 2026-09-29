using System.Diagnostics;

namespace Waggo.Api.Infrastructure.Helpers;

/// <summary>Trace id of the current request: the same value is sent to the client and written to the logs.</summary>
internal static class TraceIdHelper
{
    public static string? Get(HttpContext? httpContext = null) =>
        Activity.Current?.TraceId.ToString() ?? httpContext?.TraceIdentifier;
}
