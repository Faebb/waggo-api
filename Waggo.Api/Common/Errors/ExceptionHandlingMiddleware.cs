using Waggo.Api.Common.Responses;
using Waggo.Domain.Common;
using Waggo.Domain.Common.Exceptions;

namespace Waggo.Api.Common.Errors;

/// <summary>
/// Centralized exception handling. It is the first middleware of the pipeline, so it catches everything.
/// - <see cref="WaggoException"/>: status from its ErrorType, body with its public Error.
/// - <see cref="BadHttpRequestException"/> (binding/format problems): 400 with a generic message.
/// - Anything else: 500 asking the user to contact the administrator with the trace code.
/// The real exception is only written to the log (Serilog); it never reaches the client.
/// </summary>
internal sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection: nothing to answer.
        }
        catch (Exception exception)
        {
            string? traceId = WaggoApiResult.TraceId(context);
            (int status, Error error) = Resolve(exception, traceId);
            Log(exception, status, error, traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(
                WaggoApiResponseFactory.FromError(error.Code, error.Message, traceId));
        }
    }

    internal static (int Status, Error Error) Resolve(Exception exception, string? traceId) => exception switch
    {
        WaggoException waggo => (WaggoApiResponseFactory.StatusCodeFor(waggo.ErrorType), waggo.Error),
        BadHttpRequestException bad => (bad.StatusCode, ApiErrors.InvalidRequest),
        _ => (StatusCodes.Status500InternalServerError, ApiErrors.Unexpected(traceId)),
    };

    private void Log(Exception exception, int status, Error error, string? traceId)
    {
        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled error {Code} (trace {TraceId})", error.Code, traceId);
        }
        else
        {
            logger.LogWarning(exception, "Handled error {Code} (trace {TraceId})", error.Code, traceId);
        }
    }
}
