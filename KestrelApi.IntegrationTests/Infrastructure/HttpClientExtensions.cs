using System.Net.Http;

namespace KestrelApi.IntegrationTests.Infrastructure;

public static class HttpClientExtensions
{
    public static HttpClient WithTestAuth(this HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.AuthorizationHeaderName, 
            TestAuthHandler.TestAuthHeaderValue);
        return client;
    }
    
    public static HttpClient WithoutAuth(this HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        
        client.DefaultRequestHeaders.Remove(TestAuthHandler.AuthorizationHeaderName);
        return client;
    }
}