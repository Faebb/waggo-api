using System.Text.Json;

namespace Waggo.Api.UnitTests.Infrastructure.Settings;

/// <summary>
/// DEV-001: the test accounts and their simple passwords live only in <c>appsettings.Development.json</c>. The base
/// <c>appsettings.json</c> is what every other environment reads, so it must never carry them.
/// </summary>
public class ProductionSettingsTests
{
    [Fact]
    public void AppSettings_BaseFile_HasNoSeedSectionSoNoTestAccountReachesProduction()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(path));

        settings.RootElement.TryGetProperty("Seed", out _).ShouldBeFalse();
    }
}
