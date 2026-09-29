using Waggo.Domain.Common;

namespace Waggo.Application.Common.Pagination;

/// <summary>Page requested by the client. Pages start at 1; default size 20, max 100.</summary>
public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static readonly Error InvalidPage = new("Pagination.InvalidPage", "La página debe ser 1 o mayor.");

    public static readonly Error InvalidPageSize = new(
        "Pagination.InvalidPageSize", $"El tamaño de página debe estar entre 1 y {MaxPageSize}.");

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;

    public static WaggoResponse<PageRequest> Create(int? page, int? pageSize)
    {
        WaggoResponse<PageRequest> response = new WaggoResponse<PageRequest>();
        int p = page ?? DefaultPage;
        int size = pageSize ?? DefaultPageSize;

        if (p < 1)
        {
            response.AddError(InvalidPage, "page");
        }

        if (size is < 1 or > MaxPageSize)
        {
            response.AddError(InvalidPageSize, "pageSize");
        }

        return response.IsValid ? response.SetValue(new PageRequest(p, size)) : response;
    }
}
