using Waggo.Domain.Common;

namespace Waggo.Domain.UnitTests.Common;

public class WaggoResponseTests
{
    private static readonly Error SomeError = new("Test.Error", "Something went wrong", ErrorType.NotFound);

    [Fact]
    public void New_HasEmptyStacks_AndIsSuccess()
    {
        var response = new WaggoResponse();

        response.IsSuccess.ShouldBeTrue();
        response.Errors.ShouldBeEmpty();
        response.Warnings.ShouldBeEmpty();
        response.Infos.ShouldBeEmpty();
        response.ErrorType.ShouldBe(ErrorType.None);
    }

    [Fact]
    public void AddError_MakesItAFailure_WithTheErrorType()
    {
        var response = new WaggoResponse().AddError(SomeError, field: "id");

        response.IsFailure.ShouldBeTrue();
        response.ErrorType.ShouldBe(ErrorType.NotFound);
        response.Errors.Single().Field.ShouldBe("id");
        response.HasError("Test.Error").ShouldBeTrue();
    }

    [Fact]
    public void WarningsAndInfos_DoNotMakeItAFailure()
    {
        var response = new WaggoResponse()
            .AddWarning("W.1", "careful")
            .AddInfo("I.1", "fyi", MessageVisibility.Internal);

        response.IsSuccess.ShouldBeTrue();
        response.Warnings.Single().Code.ShouldBe("W.1");
        response.Infos.Single().Visibility.ShouldBe(MessageVisibility.Internal);
    }

    [Fact]
    public void ConcatStacks_AppendsTheThreeStacksInOrder()
    {
        var inner = new WaggoResponse()
            .AddError("E.2", "inner error")
            .AddWarning("W.2", "inner warning")
            .AddInfo("I.2", "inner info");
        var outer = new WaggoResponse()
            .AddError("E.1", "outer error")
            .AddWarning("W.1", "outer warning");

        outer.ConcatStacks(inner);

        outer.Errors.Select(e => e.Code).ShouldBe(["E.1", "E.2"]);
        outer.Warnings.Select(w => w.Code).ShouldBe(["W.1", "W.2"]);
        outer.Infos.Select(i => i.Code).ShouldBe(["I.2"]);
    }

    [Fact]
    public void ConcatStacks_KeepsTheLoggedStateOfEachMessage()
    {
        var inner = new WaggoResponse().AddWarning("W.1", "already logged");
        inner.Warnings[0].MarkAsLogged();
        var outer = new WaggoResponse().AddInfo("I.1", "pending");

        outer.ConcatStacks(inner);

        outer.PendingLogMessages().Select(m => m.Code).ShouldBe(["I.1"]);
    }

    [Fact]
    public void ConcatStacks_WithItself_DoesNotDuplicate()
    {
        var response = new WaggoResponse().AddInfo("I.1", "once");

        response.ConcatStacks(response);

        response.Infos.Count.ShouldBe(1);
    }

    [Fact]
    public void Generic_ImplicitFromValue_IsSuccessWithValue()
    {
        WaggoResponse<int> response = 42;

        response.IsSuccess.ShouldBeTrue();
        response.HasValue.ShouldBeTrue();
        response.Value.ShouldBe(42);
    }

    [Fact]
    public void Generic_ImplicitFromError_IsFailure_AndValueThrows()
    {
        WaggoResponse<int> response = SomeError;

        response.IsFailure.ShouldBeTrue();
        response.ValueOrDefault.ShouldBe(0);
        Should.Throw<InvalidOperationException>(() => response.Value);
    }

    [Fact]
    public void Generic_FluentMethods_KeepTheGenericType()
    {
        var response = new WaggoResponse<string>()
            .AddWarning("W.1", "careful")
            .AddInfo("I.1", "fyi")
            .SetValue("done");

        response.Value.ShouldBe("done");
        response.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue_AndKeepsStacks()
    {
        var response = new WaggoResponse<int>().AddWarning("W.1", "careful").SetValue(21);

        var mapped = response.Map(x => x * 2);

        mapped.Value.ShouldBe(42);
        mapped.HasWarning("W.1").ShouldBeTrue();
    }

    [Fact]
    public void Map_OnFailure_PropagatesErrors_WithoutValue()
    {
        WaggoResponse<int> response = SomeError;

        var mapped = response.Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        mapped.IsFailure.ShouldBeTrue();
        mapped.HasValue.ShouldBeFalse();
        mapped.HasError("Test.Error").ShouldBeTrue();
    }

    [Fact]
    public void ToResponse_CarriesStacksToAnotherType()
    {
        var response = new WaggoResponse<int>().AddError(SomeError);

        var other = response.ToResponse<string>();

        other.HasError("Test.Error").ShouldBeTrue();
        other.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void WaggoMessage_RequiresCodeAndMessage() =>
        Should.Throw<ArgumentException>(() => new WaggoMessage("", "text"));
}
