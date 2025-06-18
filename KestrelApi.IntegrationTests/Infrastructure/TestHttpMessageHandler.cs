using System.Net;
using System.Text;

namespace KestrelApi.IntegrationTests.Infrastructure;

public class TestHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // Mock response for Auth0 well-known endpoint
        if (request.RequestUri?.ToString().Contains("/.well-known/openid-configuration", StringComparison.OrdinalIgnoreCase) == true)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"issuer":"https://test.auth0.com/","authorization_endpoint":"https://test.auth0.com/authorize"}""",
                    Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope - HttpClient will dispose the handler
        var handler = new TestHttpMessageHandler();
        return new HttpClient(handler, disposeHandler: true);
#pragma warning restore CA2000
    }
}