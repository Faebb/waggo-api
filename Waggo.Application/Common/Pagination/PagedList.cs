namespace Waggo.Application.Common.Pagination;

/// <summary>One page of results plus the data the API needs to build its paginator.</summary>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, PageRequest request, int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);

        Items = items;
        Page = request.Page;
        PageSize = request.PageSize;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalItems { get; }

    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}

public static class PagedList
{
    /// <summary>Pages an in-memory sequence. An EF Core (IQueryable) version will live in Infrastructure with the first paged query.</summary>
    public static PagedList<T> From<T>(IEnumerable<T> source, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        var all = source as IReadOnlyCollection<T> ?? [.. source];
        var items = all.Skip(request.Skip).Take(request.PageSize).ToList();
        return new PagedList<T>(items, request, all.Count);
    }
}
