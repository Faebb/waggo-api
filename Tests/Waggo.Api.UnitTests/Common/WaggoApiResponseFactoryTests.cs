using Microsoft.AspNetCore.Http;
using Waggo.Api.Common.Responses;
using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.UnitTests.Common;

public class WaggoApiResponseFactoryTests
{
    [Fact]
    public void From_Valid_HasDataAndNullPagination()
    {
        WaggoResponse<string> response = new() { Data = "ok" };

        WaggoApiResponse<string> api = WaggoApiResponseFactory.From(response, "trace-1");

        api.Success.ShouldBeTrue();
        api.Data.ShouldBe("ok");
        api.Pagination.ShouldBeNull();
        api.TraceId.ShouldBe("trace-1");
    }

    [Fact]
    public void From_OnlySendsPublicMessages()
    {
        WaggoResponse<string> response = new() { Data = "ok" };
        response.AddWarning("W.public", "visible");
        response.AddWarning("W.internal", "log only", MessageVisibility.Internal);
        response.AddInfo("I.internal", "log only", MessageVisibility.Internal);

        WaggoApiResponse<string> api = WaggoApiResponseFactory.From(response, null);

        api.Warnings.Select(w => w.Code).ShouldBe(["W.public"]);
        api.Infos.ShouldBeEmpty();
    }

    [Fact]
    public void From_Invalid_HasNoData_AndCarriesTheErrors()
    {
        WaggoResponse<string> response = new() { Data = "ignored" };
        response.AddError("Pricing.InvalidDuration", "bad");

        WaggoApiResponse<string> api = WaggoApiResponseFactory.From(response, null);

        api.Success.ShouldBeFalse();
        api.Data.ShouldBeNull();
        api.Errors.Single().ShouldBe(new WaggoApiMessage("Pricing.InvalidDuration", "bad"));
    }

    [Fact]
    public void FromPaged_FillsPagination_AndDataIsTheItems()
    {
        PagedList<int> page = PagedList.From(Enumerable.Range(1, 45), PageRequest.Create(2, 20).Data);
        WaggoResponse<PagedList<int>> response = new() { Data = page };

        WaggoApiResponse<IReadOnlyList<int>> api = WaggoApiResponseFactory.FromPaged(response, null);

        api.Data!.Count.ShouldBe(20);
        api.Pagination.ShouldBe(new WaggoApiPagination(2, 20, 45, 3, HasPrevious: true, HasNext: true));
    }

    [Fact]
    public void FromPaged_Invalid_HasNullPagination()
    {
        WaggoResponse<PagedList<int>> response = new();
        response.ConcatStacks(PageRequest.Create(0, 20));

        WaggoApiResponse<IReadOnlyList<int>> api = WaggoApiResponseFactory.FromPaged(response, null);

        api.Pagination.ShouldBeNull();
        api.Errors.Single().Code.ShouldBe("Pagination.InvalidPage");
    }

    [Theory]
    [InlineData(ErrorType.None, StatusCodes.Status200OK)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    public void StatusCodeFor_MapsTheErrorType(ErrorType type, int status) =>
        WaggoApiResponseFactory.StatusCodeFor(type).ShouldBe(status);
}
