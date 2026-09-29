using Microsoft.Extensions.Logging;
using Waggo.Application.Common.Extensions;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Common;

namespace Waggo.Application.UnitTests.Common.Extensions;

public class WaggoResponseLoggingExtensionsTests
{
    private readonly ListLogger _logger = new();

    [Fact]
    public void WriteLogs_WritesEachStackWithItsLevel()
    {
        WaggoResponse<int> response = new();
        response.AddError("E.1", "boom");
        response.AddWarning("W.1", "careful");
        response.AddInfo("I.1", "fyi", MessageVisibility.Internal);

        response.WriteLogs(_logger, "Test");

        _logger.Entries.Select(e => e.Level).ShouldBe([LogLevel.Error, LogLevel.Warning, LogLevel.Information]);
        _logger.Entries[0].Text.ShouldBe("[Test] E.1: boom");
    }

    [Fact]
    public void WriteLogs_EmptyResponse_WritesNothing()
    {
        WaggoResponse<int> response = new();

        response.WriteLogs(_logger, "Test");

        _logger.Entries.ShouldBeEmpty();
    }
}
