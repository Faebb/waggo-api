using System.Text.Json.Serialization;

namespace Waggo.Api.Common.Responses;

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

public sealed record WaggoApiPagination(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

public sealed record WaggoApiMessage(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Field = null);
