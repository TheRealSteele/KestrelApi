using Xunit;

namespace KestrelApi.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public class IntegrationTestDefinition : ICollectionFixture<KestrelApiFactory>
{
}

[CollectionDefinition("Auth0Integration")]
public class Auth0IntegrationTestDefinition : ICollectionFixture<KestrelApiFactoryWithAuth0>
{
}