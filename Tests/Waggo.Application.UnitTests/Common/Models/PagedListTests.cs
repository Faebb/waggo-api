using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Models;

namespace Waggo.Application.UnitTests.Common.Models;

public class PagedListTests
{
    [Fact]
    public void From_ReturnsTheRequestedPage_AndTotals()
    {
        PagedList<int> page = Enumerable.Range(1, 45).ToPagedList(PageRequest.Create(2, 20).Data);

        page.Items.ShouldBe(Enumerable.Range(21, 20));
        page.TotalItems.ShouldBe(45);
        page.TotalPages.ShouldBe(3);
        page.HasPrevious.ShouldBeTrue();
        page.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void From_LastPage_HasNoNext()
    {
        PagedList<int> page = Enumerable.Range(1, 45).ToPagedList(PageRequest.Create(3, 20).Data);

        page.Items.Count.ShouldBe(5);
        page.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void From_EmptySource_HasZeroPages()
    {
        PagedList<int> page = Array.Empty<int>().ToPagedList(PageRequest.Create(1, 20).Data);

        page.TotalPages.ShouldBe(0);
        page.HasPrevious.ShouldBeFalse();
        page.HasNext.ShouldBeFalse();
    }
}
