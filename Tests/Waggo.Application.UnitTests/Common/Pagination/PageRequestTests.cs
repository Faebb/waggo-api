using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Application.UnitTests.Common.Pagination;

public class PageRequestTests
{
    [Fact]
    public void Create_WithoutValues_UsesDefaults()
    {
        PageRequest request = PageRequest.Create(null, null).Value;

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
        request.Skip.ShouldBe(0);
    }

    [Fact]
    public void Create_ComputesSkip() =>
        PageRequest.Create(3, 10).Value.Skip.ShouldBe(20);

    [Theory]
    [InlineData(0, 20, "Pagination.InvalidPage")]
    [InlineData(1, 0, "Pagination.InvalidPageSize")]
    [InlineData(1, 101, "Pagination.InvalidPageSize")]
    public void Create_InvalidValues_Fail(int page, int pageSize, string code) =>
        PageRequest.Create(page, pageSize).HasError(code).ShouldBeTrue();

    [Fact]
    public void Create_BothInvalid_ReportsBothErrors_WithTheirFields()
    {
        WaggoResponse<PageRequest> response = PageRequest.Create(0, 500);

        response.Errors.Select(e => e.Field).ShouldBe(["page", "pageSize"]);
    }
}
