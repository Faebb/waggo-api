namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>Separate container from <see cref="ApiCollectionDefinition"/>: this one runs with real JWT validation.</summary>
[CollectionDefinition(Name)]
public sealed class JwtPipelineCollectionDefinition : ICollectionFixture<JwtPipelineWaggoApiFactory>
{
    public const string Name = "jwt-pipeline";
}
