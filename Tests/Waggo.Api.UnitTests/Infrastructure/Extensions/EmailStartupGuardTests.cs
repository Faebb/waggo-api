using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Waggo.Api.Infrastructure.Extensions;

namespace Waggo.Api.UnitTests.Infrastructure.Extensions;

/// <summary>RF-001: Production sends real email; the log-only sender is for Development and Testing.</summary>
public class EmailStartupGuardTests
{
    private static IConfiguration Configuration(string? provider)
    {
        Dictionary<string, string?> values = [];
        if (provider is not null)
        {
            values["Email:Provider"] = provider;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static FakeEnvironment Environment(string name) => new(name);

    [Theory]
    [InlineData("Development")]
    [InlineData(null)]
    public void Ensure_ProductionWithoutSes_RefusesToStart(string? provider)
    {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => EmailStartupGuard.EnsureRealProviderInProduction(Configuration(provider), Environment("Production")));

        exception.Message.ShouldContain("Email:Provider");
    }

    [Theory]
    [InlineData("Ses")]
    [InlineData("ses")]
    public void Ensure_ProductionWithSes_Passes(string provider)
    {
        Should.NotThrow(
            () => EmailStartupGuard.EnsureRealProviderInProduction(Configuration(provider), Environment("Production")));
    }

    [Theory]
    [InlineData("Development", "Development")]
    [InlineData("Development", null)]
    [InlineData("Testing", "Development")]
    [InlineData("Staging", "Development")]
    public void Ensure_NotProduction_AllowsTheLogOnlySender(string environment, string? provider)
    {
        Should.NotThrow(
            () => EmailStartupGuard.EnsureRealProviderInProduction(Configuration(provider), Environment(environment)));
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Waggo.Api";

        public string ContentRootPath { get; set; } = string.Empty;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
