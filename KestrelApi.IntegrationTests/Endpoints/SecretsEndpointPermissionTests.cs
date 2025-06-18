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
public class SecretsEndpointPermissionTests : IClassFixture<KestrelApiFactoryWithAuth0>, IDisposable
{
    private readonly KestrelApiFactoryWithAuth0 _factory;
    private readonly HttpClient _client;
    private readonly TestJwtGenerator _jwtGenerator;

    public SecretsEndpointPermissionTests(KestrelApiFactoryWithAuth0 factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jwtGenerator = new TestJwtGenerator(_factory.StubServer);
    }

    [Fact]
    public async Task Post_Secrets_WithWriteSecretsPermission_ShouldReturn201()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            userId: "user-with-write",
            additionalClaims: new[] { new Claim("permissions", "write:secrets") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_Secrets_WithMultiplePermissionsIncludingWrite_ShouldReturn201()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            userId: "user-with-multiple-perms",
            additionalClaims: new[] 
            { 
                new Claim("permissions", "read:secrets"),
                new Claim("permissions", "write:secrets"),
                new Claim("permissions", "read:names")
            });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_Secrets_WithoutWriteSecretsPermission_ShouldReturn403()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            userId: "user-without-write",
            additionalClaims: new[] { new Claim("permissions", "read:secrets") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_Secrets_WithNoPermissions_ShouldReturn403()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(userId: "user-no-perms");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_Secrets_WithEmptyPermissionsClaim_ShouldReturn403()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            userId: "user-empty-perms",
            additionalClaims: new[] { new Claim("permissions", "") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_Secrets_WithCommaSeparatedPermissions_ShouldReturn201()
    {
        // Arrange
        var token = _jwtGenerator.GenerateToken(
            userId: "user-comma-perms",
            additionalClaims: new[] { new Claim("permissions", "read:secrets,write:secrets,read:names") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_Secrets_WithoutWritePermission_ShouldStillReturn200()
    {
        // Arrange
        // GET should work for any authenticated user (no special permission required)
        var token = _jwtGenerator.GenerateToken(
            userId: "user-read-only",
            additionalClaims: new[] { new Claim("permissions", "read:secrets") });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Secrets_WithNoPermissions_ShouldStillReturn200()
    {
        // Arrange
        // GET should work for any authenticated user
        var token = _jwtGenerator.GenerateToken(userId: "user-no-perms-get");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/secrets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Secrets_WithExpiredTokenButValidPermission_ShouldReturn401()
    {
        // Arrange
        // Even with correct permission, expired token should fail
        var token = _jwtGenerator.GenerateExpiredToken(userId: "user-expired");
        // Need to manually add permissions claim to expired token
        // For now, this will just test that expired tokens are rejected
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var request = new { secret = "my-secret-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/secrets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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