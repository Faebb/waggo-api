using Waggo.Application.Common.Models;
using Waggo.Domain.Common;

namespace Waggo.Application.UnitTests.Common.Models;

public class PageRequestTests
{
    [Fact]
    public void Create_WithoutValues_UsesDefaults()
    {
        PageRequest request = PageRequest.Create(null, null).Data;

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
        request.Skip.ShouldBe(0);
    }

    [Fact]
    public void Create_ComputesSkip() =>
        PageRequest.Create(3, 10).Data.Skip.ShouldBe(20);

    [Theory]
    [InlineData(0, 20, "Pagination.InvalidPage")]
    [InlineData(1, 0, "Pagination.InvalidPageSize")]
    [InlineData(1, 101, "Pagination.InvalidPageSize")]
    public void Create_InvalidValues_Fail(int page, int pageSize, string code) =>
        PageRequest.Create(page, pageSize).Errors.ShouldContain(e => e.Code == code);

    [Fact]
    public void Create_BothInvalid_ReportsBothErrors()
    {
        WaggoResponse<PageRequest> response = PageRequest.Create(0, 500);

        response.Errors.Select(e => e.Code).ShouldBe(["Pagination.InvalidPage", "Pagination.InvalidPageSize"]);
    }
}
