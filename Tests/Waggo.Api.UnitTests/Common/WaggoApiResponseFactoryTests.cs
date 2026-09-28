using Microsoft.AspNetCore.Http;
using Waggo.Api.Common.Responses;
using Waggo.Application.Common.Pagination;
using Waggo.Domain.Common;

namespace Waggo.Api.UnitTests.Common;

public class WaggoApiResponseFactoryTests
{
    [Fact]
    public void From_Success_HasDataAndNullPagination()
    {
        var response = new WaggoResponse<string>().SetValue("ok");

        var api = WaggoApiResponseFactory.From(response, "trace-1");

        api.Success.ShouldBeTrue();
        api.Data.ShouldBe("ok");
        api.Pagination.ShouldBeNull();
        api.TraceId.ShouldBe("trace-1");
    }

    [Fact]
    public void From_OnlySendsPublicMessages()
    {
        var response = new WaggoResponse<string>()
            .AddWarning("W.public", "visible")
            .AddWarning("W.internal", "log only", MessageVisibility.Internal)
            .AddInfo("I.internal", "log only", MessageVisibility.Internal)
            .SetValue("ok");

        var api = WaggoApiResponseFactory.From(response, null);

        api.Warnings.Select(w => w.Code).ShouldBe(["W.public"]);
        api.Infos.ShouldBeEmpty();
    }

    [Fact]
    public void From_Failure_HasNoData_AndCarriesErrorsWithField()
    {
        var response = new WaggoResponse<string>()
            .AddError("Pricing.InvalidDuration", "bad", field: "durationMinutes");

        var api = WaggoApiResponseFactory.From(response, null);

        api.Success.ShouldBeFalse();
        api.Data.ShouldBeNull();
        api.Errors.Single().ShouldBe(new WaggoApiMessage("Pricing.InvalidDuration", "bad", "durationMinutes"));
    }

    [Fact]
    public void FromPaged_FillsPagination_AndDataIsTheItems()
    {
        var page = PagedList.From(Enumerable.Range(1, 45), PageRequest.Create(2, 20).Value);

        var api = WaggoApiResponseFactory.FromPaged(WaggoResponse.Success(page), null);

        api.Data!.Count.ShouldBe(20);
        api.Pagination.ShouldBe(new WaggoApiPagination(2, 20, 45, 3, HasPrevious: true, HasNext: true));
    }

    [Fact]
    public void FromPaged_Failure_HasNullPagination()
    {
        var response = PageRequest.Create(0, 20).ToResponse<PagedList<int>>();

        var api = WaggoApiResponseFactory.FromPaged(response, null);

        api.Pagination.ShouldBeNull();
        api.Errors.Single().Code.ShouldBe("Pagination.InvalidPage");
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    public void StatusCodeFor_MapsTheFirstErrorType(ErrorType type, int status) =>
        WaggoApiResponseFactory.StatusCodeFor(new WaggoResponse().AddError("X", "x", type)).ShouldBe(status);

    [Fact]
    public void StatusCodeFor_Success_Is200() =>
        WaggoApiResponseFactory.StatusCodeFor(new WaggoResponse()).ShouldBe(StatusCodes.Status200OK);
}
