using Microsoft.Extensions.Logging;
using Waggo.Application.Common.Logging;
using Waggo.Domain.Common;

namespace Waggo.Application.UnitTests.Common;

public class WaggoResponseLoggingTests
{
    private readonly ListLogger _logger = new();

    [Fact]
    public void WriteLogs_WritesEachStackWithItsLevel()
    {
        WaggoResponse response = new WaggoResponse()
            .AddError("E.1", "boom")
            .AddWarning("W.1", "careful")
            .AddInfo("I.1", "fyi", MessageVisibility.Internal);

        response.WriteLogs(_logger, "Test");

        _logger.Entries.Select(e => e.Level).ShouldBe([LogLevel.Error, LogLevel.Warning, LogLevel.Information]);
        _logger.Entries[0].Text.ShouldContain("[Test] E.1: boom");
    }

    [Fact]
    public void WriteLogs_Twice_DoesNotDuplicateEntries()
    {
        WaggoResponse response = new WaggoResponse().AddWarning("W.1", "careful");

        response.WriteLogs(_logger);
        response.WriteLogs(_logger);

        _logger.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void WriteLogs_AfterConcatStacks_OnlyWritesTheNewMessages()
    {
        WaggoResponse inner = new WaggoResponse().AddWarning("W.inner", "logged by the inner method");
        inner.WriteLogs(_logger, "Inner");
        WaggoResponse outer = new WaggoResponse().AddInfo("I.outer", "pending").ConcatStacks(inner);

        outer.WriteLogs(_logger, "Outer");

        _logger.Entries.Count.ShouldBe(2);
        _logger.Entries[1].Text.ShouldContain("I.outer");
        outer.PendingLogMessages().ShouldBeEmpty();
    }

    [Fact]
    public void WriteLogs_Generic_ReturnsSameResponseForChaining()
    {
        WaggoResponse<int> response = new WaggoResponse<int>().SetValue(1);

        response.WriteLogs(_logger).ShouldBeSameAs(response);
    }
}
