using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Domain.UnitTests.Common;

public class WaggoResponseTests
{
    private static readonly Error s_notFound = new("Test.NotFound", "Not found", ErrorType.NotFound);

    [Fact]
    public void New_HasEmptyStacks_AndIsValid()
    {
        WaggoResponse<int> response = new();

        response.IsValid.ShouldBeTrue();
        response.Errors.ShouldBeEmpty();
        response.Warnings.ShouldBeEmpty();
        response.Infos.ShouldBeEmpty();
        response.ErrorType.ShouldBe(ErrorType.None);
    }

    [Fact]
    public void AddError_MakesItInvalid_AndKeepsTheErrorType()
    {
        WaggoResponse<int> response = new();

        response.AddError(s_notFound);

        response.IsValid.ShouldBeFalse();
        response.ErrorType.ShouldBe(ErrorType.NotFound);
        response.Errors.Single().Code.ShouldBe("Test.NotFound");
    }

    [Fact]
    public void AddError_ErrorTypeComesFromTheFirstError()
    {
        WaggoResponse<int> response = new();

        response.AddError("E.1", "first", ErrorType.Conflict);
        response.AddError("E.2", "second", ErrorType.NotFound);

        response.ErrorType.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void WarningsAndInfos_KeepItValid()
    {
        WaggoResponse<int> response = new();

        response.AddWarning("W.1", "careful");
        response.AddInfo("I.1", "fyi", MessageVisibility.Internal);

        response.IsValid.ShouldBeTrue();
        response.Warnings.Single().Code.ShouldBe("W.1");
        response.Infos.Single().Visibility.ShouldBe(MessageVisibility.Internal);
    }

    [Fact]
    public void Data_IsSetDirectly()
    {
        WaggoResponse<string> response = new() { Data = "done" };

        response.Data.ShouldBe("done");
    }

    [Fact]
    public void ConcatStacks_PassesTheThreeStacks_InOrder()
    {
        WaggoResponse<string> inner = new();
        inner.AddError("E.2", "inner error");
        inner.AddWarning("W.2", "inner warning");
        inner.AddInfo("I.2", "inner info");

        WaggoResponse<int> outer = new();
        outer.AddError("E.1", "outer error");
        outer.AddWarning("W.1", "outer warning");

        outer.ConcatStacks(inner);

        outer.Errors.Select(e => e.Code).ShouldBe(["E.1", "E.2"]);
        outer.Warnings.Select(w => w.Code).ShouldBe(["W.1", "W.2"]);
        outer.Infos.Select(i => i.Code).ShouldBe(["I.2"]);
    }

    [Fact]
    public void ConcatStacks_FromAnInvalidResponse_MakesItInvalidWithTheSameErrorType()
    {
        WaggoResponse<string> inner = new();
        inner.AddError(s_notFound);
        WaggoResponse<int> outer = new();

        outer.ConcatStacks(inner);

        outer.IsValid.ShouldBeFalse();
        outer.ErrorType.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void ConcatStacks_DoesNotCopyData()
    {
        WaggoResponse<int> inner = new() { Data = 42 };
        WaggoResponse<int> outer = new();

        outer.ConcatStacks(inner);

        outer.Data.ShouldBe(0);
    }
}
