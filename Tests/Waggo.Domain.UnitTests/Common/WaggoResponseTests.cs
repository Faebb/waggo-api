using Waggo.Domain.Common;

namespace Waggo.Domain.UnitTests.Common;

public class WaggoResponseTests
{
    private static readonly Error s_someError = new("Test.Error", "Something went wrong", ErrorType.NotFound);

    [Fact]
    public void New_HasEmptyStacks_AndIsValid()
    {
        WaggoResponse response = new WaggoResponse();

        response.IsValid.ShouldBeTrue();
        response.Errors.ShouldBeEmpty();
        response.Warnings.ShouldBeEmpty();
        response.Infos.ShouldBeEmpty();
        response.ErrorType.ShouldBe(ErrorType.None);
    }

    [Fact]
    public void AddError_MakesItInvalid_WithTheErrorType()
    {
        WaggoResponse response = new WaggoResponse().AddError(s_someError, field: "id");

        response.IsValid.ShouldBeFalse();
        response.ErrorType.ShouldBe(ErrorType.NotFound);
        response.Errors.Single().Field.ShouldBe("id");
        response.HasError("Test.Error").ShouldBeTrue();
    }

    [Fact]
    public void WarningsAndInfos_KeepItValid()
    {
        WaggoResponse response = new WaggoResponse()
            .AddWarning("W.1", "careful")
            .AddInfo("I.1", "fyi", MessageVisibility.Internal);

        response.IsValid.ShouldBeTrue();
        response.Warnings.Single().Code.ShouldBe("W.1");
        response.Infos.Single().Visibility.ShouldBe(MessageVisibility.Internal);
    }

    [Fact]
    public void ConcatStacks_AppendsTheThreeStacksInOrder()
    {
        WaggoResponse inner = new WaggoResponse()
            .AddError("E.2", "inner error")
            .AddWarning("W.2", "inner warning")
            .AddInfo("I.2", "inner info");
        WaggoResponse outer = new WaggoResponse()
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
        WaggoResponse inner = new WaggoResponse().AddWarning("W.1", "already logged");
        inner.Warnings[0].MarkAsLogged();
        WaggoResponse outer = new WaggoResponse().AddInfo("I.1", "pending");

        outer.ConcatStacks(inner);

        outer.PendingLogMessages().Select(m => m.Code).ShouldBe(["I.1"]);
    }

    [Fact]
    public void ConcatStacks_WithItself_DoesNotDuplicate()
    {
        WaggoResponse response = new WaggoResponse().AddInfo("I.1", "once");

        response.ConcatStacks(response);

        response.Infos.Count.ShouldBe(1);
    }

    [Fact]
    public void Generic_ImplicitFromValue_IsValidWithValue()
    {
        WaggoResponse<int> response = 42;

        response.IsValid.ShouldBeTrue();
        response.HasValue.ShouldBeTrue();
        response.Value.ShouldBe(42);
    }

    [Fact]
    public void Generic_ImplicitFromError_IsNotValid_AndValueThrows()
    {
        WaggoResponse<int> response = s_someError;

        response.IsValid.ShouldBeFalse();
        response.ValueOrDefault.ShouldBe(0);
        Should.Throw<InvalidOperationException>(() => response.Value);
    }

    [Fact]
    public void Generic_FluentMethods_KeepTheGenericType()
    {
        WaggoResponse<string> response = new WaggoResponse<string>()
            .AddWarning("W.1", "careful")
            .AddInfo("I.1", "fyi")
            .SetValue("done");

        response.Value.ShouldBe("done");
        response.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void Map_WhenValid_TransformsValue_AndKeepsStacks()
    {
        WaggoResponse<int> response = new WaggoResponse<int>().AddWarning("W.1", "careful").SetValue(21);

        WaggoResponse<int> mapped = response.Map(x => x * 2);

        mapped.Value.ShouldBe(42);
        mapped.HasWarning("W.1").ShouldBeTrue();
    }

    [Fact]
    public void Map_WhenInvalid_PropagatesErrors_WithoutValue()
    {
        WaggoResponse<int> response = s_someError;

        WaggoResponse<string> mapped = response.Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        mapped.IsValid.ShouldBeFalse();
        mapped.HasValue.ShouldBeFalse();
        mapped.HasError("Test.Error").ShouldBeTrue();
    }

    [Fact]
    public void ToResponse_CarriesStacksToAnotherType()
    {
        WaggoResponse<int> response = new WaggoResponse<int>().AddError(s_someError);

        WaggoResponse<string> other = response.ToResponse<string>();

        other.HasError("Test.Error").ShouldBeTrue();
        other.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void FromValue_IsValidWithValue()
    {
        WaggoResponse<int> response = WaggoResponse.FromValue(7);

        response.IsValid.ShouldBeTrue();
        response.Value.ShouldBe(7);
    }

    [Fact]
    public void FromError_IsNotValid()
    {
        WaggoResponse<int> response = WaggoResponse.FromError<int>(s_someError);

        response.IsValid.ShouldBeFalse();
        response.HasError("Test.Error").ShouldBeTrue();
    }

    [Fact]
    public void WaggoMessage_RequiresCodeAndMessage() =>
        Should.Throw<ArgumentException>(() => new WaggoMessage("", "text"));
}
