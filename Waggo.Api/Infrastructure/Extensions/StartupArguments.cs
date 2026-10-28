namespace Waggo.Api.Infrastructure.Extensions;

/// <summary>
/// Command-line modes of the API image. The deploy runs <c>dotnet Waggo.Api.dll --migrate-only</c> as a one-off task
/// to apply the database migrations (INF-001); the application itself never migrates outside Development and Testing.
/// </summary>
public static class StartupArguments
{
    private const string MigrateOnlyFlag = "--migrate-only";

    public static bool IsMigrateOnly(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args.Contains(MigrateOnlyFlag, StringComparer.Ordinal);
    }
}
