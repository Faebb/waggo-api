using Waggo.Application.Common.Pagination;

namespace Waggo.Application.UnitTests.Common.Pagination;

public class PagedListTests
{
    [Fact]
    public void From_ReturnsTheRequestedPage_AndTotals()
    {
        PagedList<int> page = PagedList.From(Enumerable.Range(1, 45), PageRequest.Create(2, 20).Value);

        page.Items.ShouldBe(Enumerable.Range(21, 20));
        page.TotalItems.ShouldBe(45);
        page.TotalPages.ShouldBe(3);
        page.HasPrevious.ShouldBeTrue();
        page.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void From_LastPage_HasNoNext()
    {
        PagedList<int> page = PagedList.From(Enumerable.Range(1, 45), PageRequest.Create(3, 20).Value);

        page.Items.Count.ShouldBe(5);
        page.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void From_EmptySource_HasZeroPages()
    {
        PagedList<int> page = PagedList.From(Array.Empty<int>(), PageRequest.Create(1, 20).Value);

        page.TotalPages.ShouldBe(0);
        page.HasPrevious.ShouldBeFalse();
        page.HasNext.ShouldBeFalse();
    }
}
