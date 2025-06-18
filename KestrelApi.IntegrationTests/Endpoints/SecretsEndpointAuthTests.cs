using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using KestrelApi.IntegrationTests.Infrastructure;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

[Collection("Auth0Integration")]
public class SecretsEndpointAuthTests : IClassFixture<KestrelApiFactoryWithAuth0>, IDisposable
{
    private readonly KestrelApiFactoryWithAuth0 _factory;
    private readonly HttpClient _client;
    private readonly TestJwtGenerator _jwtGenerator;

    public SecretsEndpointAuthTests(KestrelApiFactoryWithAuth0 factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jwtGenerator = new TestJwtGenerator(_factory.StubServer);
    }

    [Fact]
    public async Task Post_WithValidJwt_ReturnsCreated()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            additionalClaims: new[] { new Claim("permissions", "write:secrets") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = _jwtGenerator.GenerateExpiredToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithWrongAudience_ReturnsUnauthorized()
    {
        // Arrange
        var token = _jwtGenerator.GenerateTokenWithWrongAudience();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithWrongIssuer_ReturnsUnauthorized()
    {
        // Arrange
        var token = _jwtGenerator.GenerateTokenWithWrongIssuer();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithInvalidSignature_ReturnsUnauthorized()
    {
        // Arrange
        var token = _jwtGenerator.GenerateTokenWithInvalidSignature();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new { secret = "test-secret" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithValidJwt_ReturnsOk()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            additionalClaims: new[] { new Claim("permissions", "write:secrets") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First, post a secret
        var postRequest = new { secret = "test-secret" };
        await _client.PostAsJsonAsync("/secrets", postRequest);

        // Act
        var response = await _client.GetAsync("/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var secrets = await response.Content.ReadFromJsonAsync<string[]>();
        secrets.Should().NotBeNull();
        secrets.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MultipleRequests_WithDifferentUsers_ReturnsDifferentSecrets()
    {
        // Arrange
        var user1Token = _jwtGenerator.GenerateToken(userId: "user1", email: "user1@test.com",
            additionalClaims: new[] { new Claim("permissions", "write:secrets") });
        var user2Token = _jwtGenerator.GenerateToken(userId: "user2", email: "user2@test.com",
            additionalClaims: new[] { new Claim("permissions", "write:secrets") });

        // Post secret as user1
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        await _client.PostAsJsonAsync("/secrets", new { secret = "user1-secret" });

        // Post secret as user2
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        await _client.PostAsJsonAsync("/secrets", new { secret = "user2-secret" });

        // Act - Get secrets as user1
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var user1Response = await _client.GetAsync("/secrets");
        var user1Secrets = await user1Response.Content.ReadFromJsonAsync<string[]>();

        // Act - Get secrets as user2
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        var user2Response = await _client.GetAsync("/secrets");
        var user2Secrets = await user2Response.Content.ReadFromJsonAsync<string[]>();

        // Assert
        user1Secrets.Should().NotBeNull();
        user2Secrets.Should().NotBeNull();
        
        // Each user should only see their own secrets
        user1Secrets.Should().HaveCount(1);
        user2Secrets.Should().HaveCount(1);
        
        // The secrets should be different (base64 encoded)
        user1Secrets![0].Should().NotBe(user2Secrets![0]);
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