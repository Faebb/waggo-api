using Waggo.Application.Common.Models;

namespace Waggo.Application.Common.Extensions;

public static class PagedListExtensions
{
    /// <summary>
    /// Pages an in-memory sequence.
    /// An EF Core (IQueryable) version will live in Infrastructure with the first paged query.
    /// </summary>
    public static PagedList<T> ToPagedList<T>(this IEnumerable<T> source, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyCollection<T> all = source as IReadOnlyCollection<T> ?? [.. source];
        List<T> items = all.Skip(request.Skip).Take(request.PageSize).ToList();
        return new PagedList<T>(items, request, all.Count);
    }
}
