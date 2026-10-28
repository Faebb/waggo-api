using Waggo.Api.Infrastructure.Extensions;

namespace Waggo.Api.UnitTests.Infrastructure.Extensions;

/// <summary>INF-001: the deploy applies the migrations with the API image itself and exits (no web server).</summary>
public class StartupArgumentsTests
{
    [Fact]
    public void IsMigrateOnly_WithFlag_ReturnsTrue()
    {
        StartupArguments.IsMigrateOnly(["--migrate-only"]).ShouldBeTrue();
    }

    [Fact]
    public void IsMigrateOnly_FlagAmongOtherArguments_ReturnsTrue()
    {
        StartupArguments.IsMigrateOnly(["--urls", "http://+:8080", "--migrate-only"]).ShouldBeTrue();
    }

    [Theory]
    [InlineData]
    [InlineData("--urls", "http://+:8080")]
    [InlineData("--migrate-only-please")]
    public void IsMigrateOnly_WithoutExactFlag_ReturnsFalse(params string[] args)
    {
        StartupArguments.IsMigrateOnly(args).ShouldBeFalse();
    }
}
