using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using KestrelApi.IntegrationTests.Infrastructure;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

[Collection("Auth0Integration")]
public class SecretsEndpointTests : IClassFixture<KestrelApiFactoryWithAuth0>, IDisposable
{
    private readonly KestrelApiFactoryWithAuth0 _factory;
    private readonly HttpClient _client;
    private readonly TestJwtGenerator _jwtGenerator;

    public SecretsEndpointTests(KestrelApiFactoryWithAuth0 factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jwtGenerator = new TestJwtGenerator(_factory.StubServer);
    }

    [Fact]
    public async Task Post_Secrets_Should_Accept_Secret_And_Return_Created()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var endpoint = "/secrets";
        var request = new { secret = "my-super-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync(endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_Secrets_Should_Return_Previously_Stored_Secrets()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postEndpoint = "/secrets";
        var getEndpoint = "/secrets";
        var secretValue = "confidential-data";
        var request = new { secret = secretValue };
        
        // Act - First post a secret
        await _client.PostAsJsonAsync(postEndpoint, request);
        
        // Act - Then get all secrets
        var response = await _client.GetAsync(getEndpoint);
        var secrets = await response.Content.ReadFromJsonAsync<string[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        secrets.Should().NotBeNull();
        secrets.Should().HaveCountGreaterThan(0);
        
        // Verify the secret is returned decrypted
        secrets.Should().Contain(secretValue);
    }

    [Fact]
    public async Task Post_Secrets_Should_Store_Multiple_Secrets_Concurrently()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postEndpoint = "/secrets";
        var getEndpoint = "/secrets";
        var secretValues = new[] { "secret-1", "secret-2", "secret-3" };
        
        // Act - Post multiple secrets concurrently
        var postTasks = secretValues.Select(secret => 
            _client.PostAsJsonAsync(postEndpoint, new { secret })
        ).ToArray();
        
        await Task.WhenAll(postTasks);
        
        // Act - Get all secrets
        var response = await _client.GetAsync(getEndpoint);
        var storedSecrets = await response.Content.ReadFromJsonAsync<string[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        storedSecrets.Should().NotBeNull();
        
        // Verify all secrets are returned decrypted
        // Storage is now per-user, so we should only see our user's secrets
        foreach (var expectedSecret in secretValues)
        {
            storedSecrets.Should().Contain(expectedSecret);
        }
    }

    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
        }
    }
}