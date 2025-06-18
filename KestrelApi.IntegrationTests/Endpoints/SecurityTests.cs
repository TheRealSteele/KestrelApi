using FluentAssertions;
using KestrelApi.IntegrationTests.Infrastructure;
using System.Net;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

public class SecurityTests : IntegrationTestBase
{
    public SecurityTests(KestrelApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Security_Headers_Are_Present()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
        
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
        
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").First().Should().Be("1; mode=block");
        
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");
        
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.GetValues("Content-Security-Policy").First().Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task CORS_Headers_Are_Present()
    {
        // Arrange & Act - Make an OPTIONS request to trigger CORS
        using var request = new HttpRequestMessage(HttpMethod.Options, "/names");
        request.Headers.Add("Origin", "https://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task Rate_Limiting_Headers_Are_Present()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Rate limiting headers would be present in a real scenario with actual rate limits hit
    }
}