using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using KestrelApi.Controllers;
using KestrelApi.IntegrationTests.Infrastructure;
using Xunit;

namespace KestrelApi.IntegrationTests.Endpoints;

[Collection("Integration")]
public class HealthEndpointTests : IntegrationTestBase
{
    public HealthEndpointTests(KestrelApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_Health_Returns_Ok_When_Healthy()
    {
        // Act
        var response = await Client.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_Health_Ready_Returns_Ok()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Get_Health_Live_Returns_Ok()
    {
        // Act
        var response = await Client.GetAsync("/health/live");
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_Endpoints_Are_Anonymous()
    {
        // Act & Assert
        // All health endpoints should work without authentication
        var healthResponse = await Client.GetAsync("/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var readyResponse = await Client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var liveResponse = await Client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Response_Contains_Expected_Structure()
    {
        // Act
        var response = await Client.GetAsync("/health");
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Status.Should().NotBeNullOrEmpty();
        content.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        content.Entries.Should().NotBeNull();
        
        // Each entry should have required properties
        foreach (var entry in content.Entries)
        {
            entry.Name.Should().NotBeNullOrEmpty();
            entry.Status.Should().NotBeNullOrEmpty();
            entry.Duration.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
            entry.Tags.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Health_Response_Content_Type_Is_Json()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}