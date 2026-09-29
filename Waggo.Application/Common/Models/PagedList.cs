namespace Waggo.Application.Common.Models;

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
