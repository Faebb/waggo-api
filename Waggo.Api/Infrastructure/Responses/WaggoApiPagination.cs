namespace Waggo.Api.Infrastructure.Responses;

/// <summary>Paginator of a paged response; null when the endpoint does not page.</summary>
public sealed record WaggoApiPagination(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);
