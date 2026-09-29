namespace Waggo.Api.Infrastructure.Responses;

/// <summary>
/// Public envelope of EVERY response of the API (success or error).
/// <see cref="Pagination"/> is <c>null</c> when the endpoint does not page. Internal messages never appear here.
/// </summary>
public sealed record WaggoApiResponse<T>(
    bool Success,
    T? Data,
    WaggoApiPagination? Pagination,
    IReadOnlyList<WaggoApiMessage> Errors,
    IReadOnlyList<WaggoApiMessage> Warnings,
    IReadOnlyList<WaggoApiMessage> Infos,
    string? TraceId);
