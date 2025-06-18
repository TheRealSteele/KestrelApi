using System.Net.Http;
using Xunit;

namespace KestrelApi.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<KestrelApiFactory>
{
    protected HttpClient Client { get; }
    protected HttpClient AuthenticatedClient { get; }
    protected KestrelApiFactory Factory { get; }

    protected IntegrationTestBase(KestrelApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        
        Factory = factory;
        
        // Create unauthenticated client
        Client = factory.CreateClient();
        
        // Create authenticated client
        AuthenticatedClient = factory.CreateClient().WithTestAuth();
    }
}