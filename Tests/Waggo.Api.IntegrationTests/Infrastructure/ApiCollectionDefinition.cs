namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>Shares one API + PostgreSQL container between all integration test classes.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<WaggoApiFactory>
{
    public const string Name = "api";
}
