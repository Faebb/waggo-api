using Waggo.Domain.Common;

namespace Waggo.Domain.UnitTests.Common;

public class ResultTests
{
    private static readonly Error SomeError = new("Test.Error", "Something went wrong");

    [Fact]
    public void Success_HasValue_AndNoError()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_AccessingValue_Throws()
    {
        Result<int> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        Result<int> result = SomeError;

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        Result<int> result = 21;

        result.Map(x => x * 2).Value.ShouldBe(42);
    }
}
