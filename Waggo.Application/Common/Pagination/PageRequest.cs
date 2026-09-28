using Waggo.Domain.Common;

namespace Waggo.Application.Common.Pagination;

/// <summary>Page requested by the client. Pages start at 1; default size 20, max 100.</summary>
public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static readonly Error InvalidPage = new("Pagination.InvalidPage", "Page must be 1 or greater.");

    public static readonly Error InvalidPageSize = new(
        "Pagination.InvalidPageSize", $"Page size must be between 1 and {MaxPageSize}.");

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
        var response = new WaggoResponse<PageRequest>();
        var p = page ?? DefaultPage;
        var size = pageSize ?? DefaultPageSize;

        if (p < 1)
        {
            response.AddError(InvalidPage, "page");
        }

        if (size is < 1 or > MaxPageSize)
        {
            response.AddError(InvalidPageSize, "pageSize");
        }

        return response.IsSuccess ? response.SetValue(new PageRequest(p, size)) : response;
    }
}
